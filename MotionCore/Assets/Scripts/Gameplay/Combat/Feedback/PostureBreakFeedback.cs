using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 在角色本体表面绘制破防期间的蓝色 Fresnel 覆盖。
    /// 通过角色资源作用域接收事件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PostureBreakFeedback : MonoBehaviour,
        IEventListener<PostureChangedEvent>,
        IEventListener<HealthChangedEvent>
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField, Tooltip("破防覆盖材质")] Material m_Material;
        [SerializeField, Range(0f, 1f), Tooltip("破防覆盖透明度")] float m_Opacity = 0.4f;

        SkinnedMeshRenderer[] m_Renderers;
        Mesh[] m_Meshes;
        MaterialPropertyBlock m_Properties;
        Posture m_PostureScope;
        Health m_HealthScope;
        IEventBus m_EventBus;
        bool m_IsVisible;

        void Awake()
        {
            m_PostureScope = GetComponentInParent<Posture>();
            m_HealthScope = GetComponentInParent<Health>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
            m_Properties = new MaterialPropertyBlock();
            m_Renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            m_Meshes = new Mesh[m_Renderers.Length];
            for (int i = 0; i < m_Meshes.Length; i++)
            {
                m_Meshes[i] = new Mesh { name = $"{m_Renderers[i].name} Posture Break" };
                m_Meshes[i].MarkDynamic();
            }

            Color color = m_Material.GetColor(ColorId);
            color.a *= m_Opacity;
            m_Properties.SetColor(ColorId, color);
        }

        void OnEnable()
        {
            m_EventBus.Subscribe<PostureChangedEvent>(m_PostureScope, this);
            m_EventBus.Subscribe<HealthChangedEvent>(m_HealthScope, this);
        }

        void OnDisable()
        {
            m_EventBus.Unsubscribe<PostureChangedEvent>(m_PostureScope, this);
            m_EventBus.Unsubscribe<HealthChangedEvent>(m_HealthScope, this);
            m_IsVisible = false;
        }

        void LateUpdate()
        {
            if (!m_IsVisible)
                return;

            for (int i = 0; i < m_Renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = m_Renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                renderer.BakeMesh(m_Meshes[i]);
                RenderParams renderParams = new(m_Material)
                {
                    layer = gameObject.layer,
                    matProps = m_Properties,
                    receiveShadows = false,
                    shadowCastingMode = ShadowCastingMode.Off,
                    worldBounds = renderer.bounds
                };

                for (int subMeshIndex = 0; subMeshIndex < m_Meshes[i].subMeshCount; subMeshIndex++)
                    Graphics.RenderMesh(renderParams, m_Meshes[i], subMeshIndex, renderer.localToWorldMatrix);
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < m_Meshes.Length; i++)
                Destroy(m_Meshes[i]);
        }

        /// <summary>
        /// 设置破防覆盖是否绘制。
        /// </summary>
        public void SetVisible(bool visible)
        {
            m_IsVisible = visible;
        }

        /// <summary>
        /// 根据当前架势是否破防切换覆盖绘制。
        /// </summary>
        public void OnEvent(PostureChangedEvent eventData)
        {
            SetVisible(eventData.IsBroken);
        }

        /// <summary>
        /// 角色死亡后关闭破防覆盖。
        /// </summary>
        public void OnEvent(HealthChangedEvent eventData)
        {
            if (!eventData.Source.IsDead)
                return;

            SetVisible(false);
        }
    }
}
