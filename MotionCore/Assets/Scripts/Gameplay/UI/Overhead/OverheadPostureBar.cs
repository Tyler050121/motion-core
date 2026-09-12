using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Combat;
using MotionCore.Gameplay.Targeting;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.UI
{
    /// <summary>
    /// 角色头顶架势条。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LockOnTarget))]
    [RequireComponent(typeof(Posture))]
    public sealed class OverheadPostureBar : MonoBehaviour,
        IWorldWidget,
        IEventListener<PostureChangedEvent>,
        IEventListener<HealthChangedEvent>
    {
        const string k_WidgetId = "OverheadPostureBar";

        [SerializeField, Tooltip("相对锁定点偏移")]
        Vector3 m_Offset;

        LockOnTarget m_Target;
        Health m_Health;
        Posture m_Posture;
        PostureBar m_Bar;
        IUIService m_UIService;
        IEventBus m_EventBus;

        public string WidgetId => k_WidgetId;
        public Transform Anchor => m_Target.LockPoint;
        public Vector3 Offset => m_Offset;

        /// <summary>
        /// 获取当前角色与事件服务。
        /// </summary>
        void Awake()
        {
            m_Target = GetComponent<LockOnTarget>();
            m_Health = GetComponentInParent<Health>();
            m_Posture = GetComponent<Posture>();
            m_UIService = ServiceLocator.Resolve<IUIService>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
        }

        /// <summary>
        /// 注册角色头顶架势条。
        /// </summary>
        void OnEnable()
        {
            m_UIService.RegisterWorldWidget(this);
        }

        /// <summary>
        /// 注销角色头顶架势条。
        /// </summary>
        void OnDisable()
        {
            m_UIService.UnregisterWorldWidget(this);
        }

        /// <summary>
        /// 绑定头顶架势条实例并监听架势变化。
        /// </summary>
        public void Bind(GameObject instance)
        {
            m_Bar = instance.GetComponent<PostureBar>();
            m_Bar.SetPostureImmediate(m_Posture.CurrentPosture, m_Posture.MaxPosture);
            m_Bar.gameObject.SetActive(!m_Health.IsDead);
            m_EventBus.Subscribe<PostureChangedEvent>(m_Posture, this);
            m_EventBus.Subscribe<HealthChangedEvent>(m_Health, this);
        }

        /// <summary>
        /// 解除头顶架势条实例和架势事件监听。
        /// </summary>
        public void Unbind()
        {
            m_EventBus.Unsubscribe<PostureChangedEvent>(m_Posture, this);
            m_EventBus.Unsubscribe<HealthChangedEvent>(m_Health, this);
            m_Bar.gameObject.SetActive(false);
            m_Bar = null;
        }

        /// <summary>
        /// 更新当前角色的世界架势条。
        /// </summary>
        public void OnEvent(PostureChangedEvent eventData)
        {
            if (m_Health.IsDead)
                return;

            m_Bar.SetPosture(eventData.CurrentPosture, eventData.MaxPosture);
        }

        /// <summary>
        /// 生命归零时隐藏架势条，未来复活时恢复并刷新显示。
        /// </summary>
        public void OnEvent(HealthChangedEvent eventData)
        {
            if (eventData.Source.IsDead)
            {
                m_Bar.gameObject.SetActive(false);
                return;
            }

            m_Bar.SetPostureImmediate(m_Posture.CurrentPosture, m_Posture.MaxPosture);
            m_Bar.gameObject.SetActive(true);
        }
    }
}
