using System;
using MotionCore.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 播放完美闪避的角色残影和短时慢动作。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PerfectDodgeFeedback : MonoBehaviour
    {
        static readonly int FadeId = Shader.PropertyToID("_Fade");

        [SerializeField, Tooltip("残影材质")] Material m_Material;
        [SerializeField, Range(1, 8), Tooltip("单次残影数量")] int m_SnapshotCount = 4;
        [SerializeField, Min(0.01f), Tooltip("残影采样间隔（真实时间）")] float m_SnapshotInterval = 0.035f;
        [SerializeField, Min(0.01f), Tooltip("单个残影持续时间（真实时间）")] float m_Lifetime = 0.28f;
        [SerializeField, Range(0.05f, 1f), Tooltip("完美闪避时的时间缩放")] float m_TimeScale = 0.3f;
        [SerializeField, Min(0.01f), Tooltip("慢动作持续时间")] float m_Duration = 0.5f;

        IGameTimeService m_GameTime;
        SkinnedMeshRenderer[] m_Renderers;
        Snapshot[] m_Snapshots = Array.Empty<Snapshot>();
        int m_NextSnapshotIndex;
        int m_RemainingSnapshots;
        float m_NextCaptureTime;

        void Awake()
        {
            m_GameTime = ServiceLocator.Resolve<IGameTimeService>();
            enabled = false;
        }

        void LateUpdate()
        {
            if (m_RemainingSnapshots > 0 && Time.unscaledTime >= m_NextCaptureTime)
                CaptureNextSnapshot();

            bool isPlaying = m_RemainingSnapshots > 0;
            for (int i = 0; i < m_Snapshots.Length; i++)
                isPlaying |= m_Snapshots[i].Draw(m_Material, gameObject.layer, m_Lifetime, Time.unscaledDeltaTime);

            if (!isPlaying)
                enabled = false;
        }

        void OnDestroy()
        {
            for (int i = 0; i < m_Snapshots.Length; i++)
                m_Snapshots[i].Dispose();
        }

        /// <summary>
        /// 播放一次完美闪避反馈。
        /// </summary>
        public void Play()
        {
            if (m_Snapshots.Length == 0)
                InitializeSnapshots();

            enabled = true;
            m_RemainingSnapshots = m_SnapshotCount;
            CaptureNextSnapshot();
            m_GameTime.PlaySlowMotion(m_TimeScale, m_Duration);
        }

        void InitializeSnapshots()
        {
            m_Renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            m_Snapshots = new Snapshot[m_SnapshotCount];
            for (int i = 0; i < m_Snapshots.Length; i++)
                m_Snapshots[i] = new Snapshot(m_Renderers);
        }

        void CaptureNextSnapshot()
        {
            m_Snapshots[m_NextSnapshotIndex].Capture(m_Renderers, m_Lifetime);
            m_NextSnapshotIndex = (m_NextSnapshotIndex + 1) % m_Snapshots.Length;
            m_RemainingSnapshots--;
            m_NextCaptureTime = Time.unscaledTime + m_SnapshotInterval;
        }

        sealed class Snapshot
        {
            readonly Mesh[] m_Meshes;
            readonly Matrix4x4[] m_Matrices;
            readonly bool[] m_VisibleParts;
            readonly MaterialPropertyBlock m_Properties = new();

            Bounds m_WorldBounds;
            float m_RemainingTime;

            public Snapshot(SkinnedMeshRenderer[] renderers)
            {
                m_Meshes = new Mesh[renderers.Length];
                m_Matrices = new Matrix4x4[renderers.Length];
                m_VisibleParts = new bool[renderers.Length];

                for (int i = 0; i < m_Meshes.Length; i++)
                {
                    m_Meshes[i] = new Mesh { name = $"{renderers[i].name} Afterimage" };
                    m_Meshes[i].MarkDynamic();
                }
            }

            public void Capture(SkinnedMeshRenderer[] renderers, float lifetime)
            {
                bool hasBounds = false;
                for (int i = 0; i < renderers.Length; i++)
                {
                    SkinnedMeshRenderer renderer = renderers[i];
                    m_VisibleParts[i] = renderer.enabled && renderer.gameObject.activeInHierarchy;
                    if (!m_VisibleParts[i])
                        continue;

                    renderer.BakeMesh(m_Meshes[i]);
                    m_Matrices[i] = renderer.localToWorldMatrix;

                    if (hasBounds)
                        m_WorldBounds.Encapsulate(renderer.bounds);
                    else
                    {
                        m_WorldBounds = renderer.bounds;
                        hasBounds = true;
                    }
                }

                m_RemainingTime = hasBounds ? lifetime : 0f;
            }

            public bool Draw(Material material, int layer, float lifetime, float unscaledDeltaTime)
            {
                m_RemainingTime -= unscaledDeltaTime;
                if (m_RemainingTime <= 0f)
                    return false;

                m_Properties.SetFloat(FadeId, m_RemainingTime / lifetime);
                RenderParams renderParams = new(material)
                {
                    layer = layer,
                    matProps = m_Properties,
                    receiveShadows = false,
                    shadowCastingMode = ShadowCastingMode.Off,
                    worldBounds = m_WorldBounds
                };

                for (int i = 0; i < m_Meshes.Length; i++)
                {
                    if (!m_VisibleParts[i])
                        continue;

                    for (int subMeshIndex = 0; subMeshIndex < m_Meshes[i].subMeshCount; subMeshIndex++)
                        Graphics.RenderMesh(renderParams, m_Meshes[i], subMeshIndex, m_Matrices[i]);
                }

                return true;
            }

            public void Dispose()
            {
                for (int i = 0; i < m_Meshes.Length; i++)
                    UnityEngine.Object.Destroy(m_Meshes[i]);
            }
        }
    }
}
