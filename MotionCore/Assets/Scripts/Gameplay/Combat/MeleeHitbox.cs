using System.Collections.Generic;
using MotionCore.Gameplay.Common;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class MeleeHitbox : MonoBehaviour
    {
        const int MaxHits = 16;
        const float GizmoHoldSeconds = 0.35f;

        [SerializeField] bool m_DrawGizmos = true;

        readonly Collider[] m_Results = new Collider[MaxHits];
        readonly HashSet<Hurtbox> m_InstantHitTargets = new();
        readonly List<HitWindow> m_ActiveWindows = new();
        readonly List<HitWindow> m_WindowPool = new();
        readonly List<GizmoSample> m_GizmoSamples = new();
        Health m_AttackerHealth;
        Hurtbox m_AttackerHurtbox;

        // TODO: 接入角色动态生成后，由统一初始化流程注入攻击者上下文，避免各命中组件独立查询层级。
        void Awake()
        {
            m_AttackerHealth = GetComponentInParent<Health>();
            m_AttackerHurtbox = m_AttackerHealth.GetComponentInChildren<Hurtbox>();
        }

        /// <summary>
        /// 打开一个持续命中窗口，同一个窗口在关闭前只会命中同一目标一次。
        /// </summary>
        public void Open(int id, HitProfile profile, Transform source, Vector3 localOffset, Transform rayOrigin)
        {
            if (FindWindowIndex(id) >= 0)
            {
                Debug.LogError($"Hit window {id} is already open.", this);
                return;
            }

            HitWindow window = GetWindow();
            window.Id = id;
            window.Profile = profile;
            window.Source = source;
            window.LocalOffset = localOffset;
            window.RayOrigin = rayOrigin;
            window.HitTargets.Clear();
            m_ActiveWindows.Add(window);
        }

        /// <summary>
        /// 执行一次瞬时命中检测。
        /// </summary>
        public void Hit(int id, HitProfile profile, Transform source, Vector3 localOffset, Transform rayOrigin)
        {
            m_InstantHitTargets.Clear();
            Sample(id, profile, source, localOffset, rayOrigin, m_InstantHitTargets);
            m_InstantHitTargets.Clear();
        }

        /// <summary>
        /// 关闭指定的持续命中窗口。
        /// </summary>
        public void Close(int id)
        {
            int index = FindWindowIndex(id);
            if (index < 0)
            {
                Debug.LogError($"Hit window {id} is not open.", this);
                return;
            }

            ReleaseWindow(index);
        }

        /// <summary>
        /// 关闭所有持续命中窗口。
        /// </summary>
        public void CloseAll()
        {
            for (int i = m_ActiveWindows.Count - 1; i >= 0; i--)
            {
                ReleaseWindow(i);
            }
        }

        void Update()
        {
            for (int i = 0; i < m_ActiveWindows.Count; i++)
            {
                HitWindow window = m_ActiveWindows[i];
                Sample(window.Id, window.Profile, window.Source, window.LocalOffset, window.RayOrigin, window.HitTargets);
            }
        }

        HitWindow GetWindow()
        {
            if (m_WindowPool.Count == 0)
                return new HitWindow();

            int index = m_WindowPool.Count - 1;
            HitWindow window = m_WindowPool[index];
            m_WindowPool.RemoveAt(index);
            return window;
        }

        void ReleaseWindow(int index)
        {
            HitWindow window = m_ActiveWindows[index];
            m_ActiveWindows.RemoveAt(index);
            window.Profile = null;
            window.Source = null;
            window.LocalOffset = default;
            window.RayOrigin = null;
            window.HitTargets.Clear();
            m_WindowPool.Add(window);
        }

        int FindWindowIndex(int id)
        {
            for (int i = 0; i < m_ActiveWindows.Count; i++)
            {
                if (m_ActiveWindows[i].Id == id)
                    return i;
            }

            return -1;
        }

        void Sample(
            int id,
            HitProfile profile,
            Transform source,
            Vector3 localOffset,
            Transform rayOrigin,
            HashSet<Hurtbox> hitTargets)
        {
            Vector3 center = source.TransformPoint(localOffset);
            m_GizmoSamples.Add(new GizmoSample(center, profile.Radius, Time.time + GizmoHoldSeconds));

            int count = Physics.OverlapSphereNonAlloc(
                center,
                profile.Radius,
                m_Results,
                profile.TargetLayers,
                QueryTriggerInteraction.Collide);

            Faction attackerFaction = m_AttackerHurtbox.Faction;

            for (int i = 0; i < count; i++)
            {
                Hurtbox hurtbox = m_Results[i].GetComponentInParent<Hurtbox>();
                if (hurtbox == null || hurtbox.Health == m_AttackerHealth)
                    continue;

                // 同阵营友伤跳过。
                if (attackerFaction == hurtbox.Faction)
                    continue;

                // 同一窗口内每个目标只结算一次。
                // 注意 Add 先于 ReceiveHit：被无敌拒绝的命中同样占用窗口，
                // 一次成功闪避作废整刀，不会在 HitStart/HitEnd 窗口尾部被补中。
                if (!hitTargets.Add(hurtbox))
                    continue;

                Vector3 direction = center - source.position;
                direction.y = 0f;
                Vector3 impactPoint = m_Results[i].ClosestPoint(center);

                // 执行攻击
                hurtbox.ReceiveHit(
                    profile,
                    ResolveHitPoint(m_Results[i], impactPoint, rayOrigin),
                    hurtbox.ResolveVisualPoint(source),
                    direction,
                    source);
            }
        }

        Vector3 ResolveHitPoint(Collider target, Vector3 impactPoint, Transform rayOrigin)
        {
            Vector3 rayDirection = impactPoint - rayOrigin.position;
            float rayDistance = rayDirection.magnitude;
            if (rayDistance <= 0.0001f)
                return impactPoint;

            Ray ray = new(rayOrigin.position, rayDirection / rayDistance);
            if (!target.Raycast(ray, out RaycastHit hit, rayDistance + 0.05f))
                return impactPoint;

            return hit.point;
        }

        void OnDrawGizmos()
        {
            if (!m_DrawGizmos)
                return;

            float now = Application.isPlaying ? Time.time : 0f;

            Gizmos.color = Color.red;
            for (int i = 0; i < m_ActiveWindows.Count; i++)
            {
                HitWindow window = m_ActiveWindows[i];
                Gizmos.DrawWireSphere(
                    window.Source.TransformPoint(window.LocalOffset),
                    window.Profile.Radius);
            }

            Gizmos.color = Color.yellow;
            for (int i = m_GizmoSamples.Count - 1; i >= 0; i--)
            {
                GizmoSample sample = m_GizmoSamples[i];
                if (Application.isPlaying && now > sample.ExpireTime)
                {
                    m_GizmoSamples.RemoveAt(i);
                    continue;
                }

                Gizmos.DrawWireSphere(sample.Center, sample.Radius);
            }
        }

        sealed class HitWindow
        {
            public int Id;
            public HitProfile Profile;
            public Transform Source;
            public Vector3 LocalOffset;
            public Transform RayOrigin;
            public readonly HashSet<Hurtbox> HitTargets = new();
        }

        readonly struct GizmoSample
        {
            public readonly Vector3 Center;
            public readonly float Radius;
            public readonly float ExpireTime;

            public GizmoSample(Vector3 center, float radius, float expireTime)
            {
                Center = center;
                Radius = radius;
                ExpireTime = expireTime;
            }
        }
    }
}
