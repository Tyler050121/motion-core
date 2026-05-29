using System.Collections.Generic;
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

        /// <summary>
        /// 打开一个持续命中窗口，同一个窗口在关闭前只会命中同一目标一次。
        /// </summary>
        public void Open(int id, HitProfile profile, Transform source)
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
            window.HitTargets.Clear();
            m_ActiveWindows.Add(window);
        }

        /// <summary>
        /// 执行一次瞬时命中检测。
        /// </summary>
        public void Hit(int id, HitProfile profile, Transform source)
        {
            m_InstantHitTargets.Clear();
            Sample(id, profile, source, m_InstantHitTargets);
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
                Sample(window.Id, window.Profile, window.Source, window.HitTargets);
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
            HashSet<Hurtbox> hitTargets)
        {
            Vector3 center = source.TransformPoint(profile.LocalOffset);
            m_GizmoSamples.Add(new GizmoSample(center, profile.Radius, Time.time + GizmoHoldSeconds));

            int count = Physics.OverlapSphereNonAlloc(
                center,
                profile.Radius,
                m_Results,
                profile.TargetLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Hurtbox hurtbox = m_Results[i].GetComponentInParent<Hurtbox>();
                if (hurtbox == null || hurtbox.transform.root == source.root)
                    continue;

                if (!hitTargets.Add(hurtbox))
                    continue;

                // 执行攻击
                HitResult result = hurtbox.ReceiveHit(profile, m_Results[i].ClosestPoint(center));
                Debug.Log(
                    $"Hit {id} {result.Hurtbox.name} for {result.Damage:0.##}. Health: {result.RemainingHealth:0.##}",
                    result.Hurtbox);
            }
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
                    window.Source.TransformPoint(window.Profile.LocalOffset),
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
