using Animancer;
using MotionCore.Gameplay.Combat;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 锁定受害者并播放配对处决反应。
    /// </summary>
    public sealed class ExecutedState : CharacterState, IExecutionTarget
    {
        [SerializeField, Tooltip("被处决动画")] TransitionAsset m_Execution;
        [SerializeField] Hurtbox m_Hurtbox;

        readonly TimerHandle m_WindowTimer = new();

        ITimerService m_Timer;
        PostureBreakFeedback m_PostureBreakFeedback;
        Posture m_Posture;

        public override CharacterStateType Type => CharacterStateType.Executed;
        public override CastPriority CurrentCastPriority => CastPriority.None;
        public override StaggerLevel CurrentStaggerLevel => StaggerLevel.Execution;
        public bool CanBeExecuted => m_WindowTimer.IsActive;

        void Awake()
        {
            m_Timer = ServiceLocator.Resolve<ITimerService>();
            m_Posture = Character.GetComponent<Posture>();
            m_PostureBreakFeedback = Character.GetComponent<PostureBreakFeedback>();
        }

        /// <summary>
        /// 开放一段不依赖角色当前状态的可处决窗口。
        /// </summary>
        public void BeginWindow(float duration, System.Action onWindowExpired)
        {
            m_Timer.Delay(this, duration, m_WindowTimer, onWindowExpired);
        }

        /// <summary>
        /// 消费当前窗口并进入被处决状态。
        /// </summary>
        public bool TryClaim()
        {
            if (!CanBeExecuted)
                return false;

            m_Timer.Remove(m_WindowTimer);
            Character.StateMachine.ForceSetState(this);
            return true;
        }

        void OnEnable()
        {
            m_PostureBreakFeedback.SetVisible(false);
            m_Hurtbox.SetInvulnerable(true);
            PlayWithEvents(m_Execution, FinishExecution);
        }

        void OnDisable()
        {
            m_Hurtbox.SetInvulnerable(false);
        }

        void FinishExecution()
        {
            m_Posture.Restore();
            ReturnToDefaultState();
        }
    }
}
