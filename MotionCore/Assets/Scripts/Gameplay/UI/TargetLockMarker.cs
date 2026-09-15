using MotionCore.Gameplay.Targeting;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.UI
{
    /// <summary>
    /// 显示当前锁定目标的世界空间标记。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TargetLockMarker : MonoBehaviour, IWorldWidget, IEventListener<TargetLockChangedEvent>
    {
        const string k_WidgetId = "TargetLockMarker";

        [SerializeField, Tooltip("相对目标锁定点的偏移")]
        Vector3 m_Offset;

        IUIService m_UIService;
        IEventBus m_EventBus;
        LockOnTarget m_Target;

        public string WidgetId => k_WidgetId;
        public Transform Anchor => m_Target.LockPoint;
        public Vector3 Offset => m_Offset;

        /// <summary>
        /// 获取 UI 服务与全局事件总线。
        /// </summary>
        void Awake()
        {
            m_UIService = ServiceLocator.Resolve<IUIService>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
        }

        /// <summary>
        /// 订阅全局锁定变化。
        /// </summary>
        void OnEnable()
        {
            m_EventBus.Subscribe<TargetLockChangedEvent>(this);
        }

        /// <summary>
        /// 解除锁定事件和世界 Widget 注册。
        /// </summary>
        void OnDisable()
        {
            m_EventBus.Unsubscribe<TargetLockChangedEvent>(this);
            SetTarget(null);
        }

        public void OnEvent(TargetLockChangedEvent eventData)
        {
            if (eventData.Source == gameObject)
                SetTarget(eventData.Target);
        }

        public void Bind(GameObject instance) { }

        public void Unbind() { }

        void SetTarget(LockOnTarget target)
        {
            if (ReferenceEquals(m_Target, target))
                return;

            if (target)
            {
                if (!m_Target)
                    m_UIService.RegisterWorldWidget(this);
            }
            else
                m_UIService.UnregisterWorldWidget(this);

            m_Target = target;
        }
    }
}
