using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Targeting;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.UI
{
    /// <summary>
    /// 角色头顶血条。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LockOnTarget))]
    public sealed class OverheadHealthBar : MonoBehaviour, IWorldWidget, IEventListener<HealthChangedEvent>
    {
        const string k_WidgetId = "OverheadHealthBar";

        [SerializeField, Tooltip("相对锁定点偏移")]
        Vector3 m_Offset;

        Health m_Health;
        LockOnTarget m_Target;
        HealthBar m_Bar;
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
            m_UIService = ServiceLocator.Resolve<IUIService>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
        }

        /// <summary>
        /// 注册角色头顶血条。
        /// </summary>
        void OnEnable()
        {
            m_UIService.RegisterWorldWidget(this);
        }

        /// <summary>
        /// 注销角色头顶血条。
        /// </summary>
        void OnDisable()
        {
            m_UIService.UnregisterWorldWidget(this);
        }

        /// <summary>
        /// 绑定头顶血条实例并监听生命变化。
        /// </summary>
        public void Bind(GameObject instance)
        {
            m_Bar = instance.GetComponent<HealthBar>();
            m_Bar.SetHealthImmediate(m_Health.CurrentHealth, m_Health.MaxHealth);
            m_EventBus.Subscribe<HealthChangedEvent>(m_Health, this);
        }

        /// <summary>
        /// 解除头顶血条实例和生命事件监听。
        /// </summary>
        public void Unbind()
        {
            m_EventBus.Unsubscribe<HealthChangedEvent>(m_Health, this);
            m_Bar = null;
        }

        /// <summary>
        /// 更新当前角色的世界血条。
        /// </summary>
        public void OnEvent(HealthChangedEvent eventData)
        {
            m_Bar.SetHealth(eventData.CurrentHealth, eventData.MaxHealth);
        }
    }
}
