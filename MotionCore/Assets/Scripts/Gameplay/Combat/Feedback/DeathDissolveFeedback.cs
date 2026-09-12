using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;
using MotionCore;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    using Character = MotionCore.Gameplay.Character.Character;

    /// <summary>
    /// 死亡角色的延迟溶解反馈。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeathDissolveFeedback : MonoBehaviour, IEventListener<HealthChangedEvent>
    {
        enum DissolvePhase
        {
            Idle,
            Delaying,
            Playing,
            Complete
        }

        [SerializeField, Min(0f), Tooltip("死亡后开始溶解的延迟")]
        float m_StartDelay = 3f;
        [SerializeField, Min(0.01f), Tooltip("溶解持续时间")]
        float m_Duration = 3f;

        Health m_Health;
        Character m_Character;
        IEventBus m_EventBus;
        ITimerService m_Timer;
        DeathDissolveView m_View;
        readonly TimerHandle m_StartDelayTimer = new();
        readonly TimerHandle m_DissolveTimer = new();
        DissolvePhase m_Phase;

        void Awake()
        {
            m_Health = GetComponentInParent<Health>();
            m_Character = GetComponentInParent<Character>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
            m_Timer = ServiceLocator.Resolve<ITimerService>();
            m_View = new DeathDissolveView(transform);
        }

        void OnEnable()
        {
            m_EventBus.Subscribe<HealthChangedEvent>(m_Health, this);

            if (m_Health.IsDead)
                BeginDissolve();
        }

        void OnDisable()
        {
            m_EventBus.Unsubscribe<HealthChangedEvent>(m_Health, this);
            m_Timer.Remove(m_StartDelayTimer);
            m_Timer.Remove(m_DissolveTimer);
            m_Phase = DissolvePhase.Idle;
            m_Character.SetCollisionEnabled(true);
            m_View.Restore();
        }

        void Update()
        {
            if (m_Phase != DissolvePhase.Playing)
                return;

            m_View.SetProgress(m_DissolveTimer.Progress);
        }

        /// <summary>
        /// 仅响应角色死亡事件并启动溶解流程。
        /// </summary>
        public void OnEvent(HealthChangedEvent eventData)
        {
            if (!m_Health.IsDead)
                return;

            BeginDissolve();
        }

        /// <summary>
        /// 响应死亡事件并启动开始溶解前的延迟。
        /// </summary>
        void BeginDissolve()
        {
            if (m_Phase != DissolvePhase.Idle)
                return;

            m_Phase = DissolvePhase.Delaying;
            m_Timer.Delay(this, m_StartDelay, m_StartDelayTimer, StartDissolve);
        }

        /// <summary>
        /// 延迟结束后关闭身体碰撞并启动溶解计时。
        /// </summary>
        void StartDissolve()
        {
            m_Character.SetCollisionEnabled(false);
            m_Phase = DissolvePhase.Playing;
            m_Timer.Delay(this, m_Duration, m_DissolveTimer, CompleteDissolve);
        }

        /// <summary>
        /// 结束溶解并关闭视觉表现，保留角色根用于复活。
        /// </summary>
        void CompleteDissolve()
        {
            m_View.SetVisible(false);
            m_Phase = DissolvePhase.Complete;
        }
    }
}
