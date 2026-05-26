using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class AttackState : CharacterState
    {
        AttackRequest m_PendingRequest;
        AttackDefinition m_CurrentAttack;
        AttackDefinition.AttackStepDefinition m_CurrentStep;
        AttackDefinition m_ComboAttack;
        CharacterStateType m_ActionType;
        int m_CurrentStepIndex;
        int m_LastStepIndexExclusive;
        int m_ComboStepIndex;
        int m_ComboLastStepIndexExclusive;
        float m_CurrentStepStartTime;
        float m_PerfectWindowAnchorTime;
        float m_ComboExpireTime;
        bool m_IsPerfectVariant;
        bool m_IsPlayingEndStep;

        protected override bool CanInterruptSelf => (ExitOptions & CharacterStateExitOptions.Attack) != 0;
        public override CharacterStateType Type => m_ActionType;

        /// <summary>
        /// 排队一次动作输入。状态未启用时会尝试恢复连段窗口；
        /// 状态播放中且是同一个动作时，会尝试推进到下一段。
        /// </summary>
        public bool QueueAttack(AttackDefinition definition)
        {
            if (!enabled)
            {
                if (CanResumeCombo(definition))
                    m_PendingRequest = new AttackRequest(
                        definition,
                        m_ComboStepIndex,
                        m_ComboLastStepIndexExclusive - m_ComboStepIndex);
                else
                    m_PendingRequest = new AttackRequest(definition);

                m_ActionType = definition.StateType;
                return true;
            }

            if (m_CurrentAttack == definition)
            {
                bool canAttack = (ExitOptions & CharacterStateExitOptions.Attack) != 0;

                int nextStepIndex = m_CurrentStepIndex + 1;
                if (nextStepIndex >= m_LastStepIndexExclusive)
                {
                    AttackRequest restartRequest = new(definition);
                    if (canAttack)
                        PlayAttack(restartRequest);
                    else
                        m_PendingRequest = restartRequest;

                    return true;
                }

                AttackRequest nextRequest = new(
                    definition,
                    nextStepIndex,
                    m_LastStepIndexExclusive - nextStepIndex,
                    ShouldUsePerfectVariant(definition, nextStepIndex));

                if (canAttack)
                    PlayAttack(nextRequest);
                else
                    m_PendingRequest = nextRequest;

                return true;
            }

            return false;
        }

        void OnEnable()
        {
            PlayAttack(m_PendingRequest);
        }

        void OnDisable()
        {
            if (m_ComboAttack == null)
                OpenComboGrace(m_CurrentStep.ComboGraceStartType, true);

            m_CurrentAttack = null;
            m_CurrentStep = null;
            m_PendingRequest = default;
            m_LastStepIndexExclusive = 0;
            m_PerfectWindowAnchorTime = 0f;
            m_IsPerfectVariant = false;
            m_IsPlayingEndStep = false;
        }

        #region 播放流程
        void PlayAttack(AttackRequest attackRequest)
        {
            m_CurrentAttack = attackRequest.Definition;
            ClearCombo();
            m_ActionType = m_CurrentAttack.StateType;
            m_CurrentStepIndex = attackRequest.StartStepIndex;
            m_LastStepIndexExclusive = attackRequest.StepCount < 0
                ? m_CurrentAttack.StepCount
                : Mathf.Min(m_CurrentStepIndex + attackRequest.StepCount, m_CurrentAttack.StepCount);
            m_CurrentAttack.TryGetStep(m_CurrentStepIndex, out m_CurrentStep);
            m_PendingRequest = default;
            m_IsPlayingEndStep = false;
            m_CurrentStepStartTime = Time.time;
            m_PerfectWindowAnchorTime = 0f;

            OpenComboGrace(AttackDefinition.ComboGraceStartType.Start);
            PlayStep(m_CurrentStep, attackRequest.UsePerfectVariant);
        }

        void PlayStep(AttackDefinition.AttackStepDefinition step, bool usePerfectVariant)
        {
            if (usePerfectVariant && step.HasPerfectVariant)
            {
                AttackDefinition.AttackStepVariantDefinition variant = step.PerfectVariant;
                m_IsPerfectVariant = true;
                PlayStep(variant.Animation, variant.EventBindings);
                return;
            }

            m_IsPerfectVariant = false;
            PlayStep(step.Animation, step.EventBindings);
        }

        void PlayStep(AttackDefinition.AttackEndStepDefinition step)
        {
            PlayStep(step.Animation, step.EventBindings);
        }

        void PlayStep(TransitionAsset animation, AttackDefinition.AttackStepDefinition.EventBinding[] eventBindings)
        {
            AnimancerState state = Character.Animancer.Play(animation);
            AnimancerEvent.Sequence events = state.Events(this);

            AttackStepEventOptions stepEventOptions = AttackStepEventOptions.None;
            foreach (AttackDefinition.AttackStepDefinition.EventBinding binding in eventBindings)
            {
                stepEventOptions |= GetStepEventOptions(binding.Type);
            }

            CharacterStateExitOptions cancelOptions = CharacterStateExitOptions.Cancel;

            ExitOptions = CharacterStateExitOptions.All;
            if ((stepEventOptions & AttackStepEventOptions.CanCancel) != 0)
                ExitOptions &= ~CharacterStateExitOptions.Cancel;
            if ((stepEventOptions & AttackStepEventOptions.CanAttack) != 0)
            {
                ExitOptions &= ~CharacterStateExitOptions.Attack;
                cancelOptions &= ~CharacterStateExitOptions.Attack;
            }

            foreach (AttackDefinition.AttackStepDefinition.EventBinding binding in eventBindings)
            {
                AttackDefinition.EventType eventType = binding.Type;
                if (eventType == AttackDefinition.EventType.CanCancel)
                    events.SetCallback(binding.Event, () => OpenCancel(cancelOptions));
                else if (eventType == AttackDefinition.EventType.CanAttack)
                    events.SetCallback(binding.Event, OpenAttack);
            }

            events.OnEnd = OnStepEnded;
        }

        /// <summary>
        /// 打开当前段的取消窗口。如果之前已经缓存了下一段输入，
        /// 这里会立即消费并切到下一段。
        /// </summary>
        void OpenCancel(CharacterStateExitOptions cancelOptions)
        {
            ExitOptions |= cancelOptions;

            if ((ExitOptions & CharacterStateExitOptions.Attack) == 0)
                return;

            m_PerfectWindowAnchorTime = Time.time;
            if (m_PendingRequest.Definition != null)
                PlayAttack(m_PendingRequest);
        }

        void OpenAttack()
        {
            m_PerfectWindowAnchorTime = Time.time;
            ExitOptions |= CharacterStateExitOptions.Attack;
            if (m_PendingRequest.Definition != null)
                PlayAttack(m_PendingRequest);
        }

        void OnStepEnded()
        {
            if (Character.StateMachine.CurrentState != this)
                return;

            if (!m_IsPlayingEndStep)
            {
                OpenComboGrace(AttackDefinition.ComboGraceStartType.End);

                if (m_IsPerfectVariant && m_CurrentStep.PerfectVariant.HasEndStep)
                {
                    m_IsPlayingEndStep = true;
                    PlayStep(m_CurrentStep.PerfectVariant.EndStep);
                    return;
                }

                if (m_CurrentStep.HasEndStep)
                {
                    m_IsPlayingEndStep = true;
                    PlayStep(m_CurrentStep.EndStep);
                    return;
                }
            }

            m_PendingRequest = default;
            ExitOptions |= CharacterStateExitOptions.Idle;
            Character.StateMachine.TrySetDefaultState();
        }

        #endregion

        #region Perfect窗口

        bool ShouldUsePerfectVariant(AttackDefinition definition, int stepIndex)
        {
            return IsInPerfectWindow()
                && definition.TryGetStep(stepIndex, out AttackDefinition.AttackStepDefinition step)
                && step.HasPerfectVariant;
        }

        bool IsInPerfectWindow()
        {
            if (m_PerfectWindowAnchorTime <= 0f)
                return false;
            
            float startTime = m_PerfectWindowAnchorTime + m_CurrentStep.PerfectWindowOffset;
            float endTime = startTime + m_CurrentStep.PerfectWindowDuration;
            return Time.time >= startTime && Time.time <= endTime;
        }

        #endregion

        #region 连段窗口

        void OpenComboGrace(AttackDefinition.ComboGraceStartType startType, bool force = false)
        {
            if (!force && m_CurrentStep.ComboGraceStartType != startType)
                return;

            int nextStepIndex = m_CurrentStepIndex + 1;
            if (nextStepIndex >= m_LastStepIndexExclusive)
                return;

            m_ComboAttack = m_CurrentAttack;
            m_ComboStepIndex = nextStepIndex;
            m_ComboLastStepIndexExclusive = m_LastStepIndexExclusive;
            m_ComboExpireTime = GetComboGraceStartTime(force) + m_CurrentStep.ComboGraceSeconds;
        }

        bool CanResumeCombo(AttackDefinition definition)
        {
            return m_ComboAttack == definition
                && Time.time <= m_ComboExpireTime
                && m_ComboStepIndex < m_ComboLastStepIndexExclusive;
        }

        float GetComboGraceStartTime(bool force)
        {
            if (!force)
                return Time.time;

            if (m_CurrentStep.ComboGraceStartType == AttackDefinition.ComboGraceStartType.End)
            {
                TransitionAsset animation = m_IsPerfectVariant && m_CurrentStep.HasPerfectVariant
                    ? m_CurrentStep.PerfectVariant.Animation
                    : m_CurrentStep.Animation;
                ITransition transition = animation.GetTransition();
                float speed = Mathf.Abs(transition.Speed);
                float duration = speed > 0f ? transition.MaximumLength / speed : transition.MaximumLength;
                return m_CurrentStepStartTime + duration;
            }

            return Time.time;
        }

        void ClearCombo()
        {
            m_ComboAttack = null;
            m_ComboStepIndex = 0;
            m_ComboLastStepIndexExclusive = 0;
            m_ComboExpireTime = 0f;
        }

        static AttackStepEventOptions GetStepEventOptions(AttackDefinition.EventType eventType)
        {
            return eventType switch
            {
                AttackDefinition.EventType.CanCancel => AttackStepEventOptions.CanCancel,
                AttackDefinition.EventType.CanAttack => AttackStepEventOptions.CanAttack,
                _ => AttackStepEventOptions.None
            };
        }

        #endregion
    }

    [System.Flags]
    enum AttackStepEventOptions
    {
        None = 0,
        CanCancel = 1 << 0,
        CanAttack = 1 << 1,
    }

    public readonly struct AttackRequest
    {
        public AttackRequest(AttackDefinition definition)
            : this(definition, 0, -1)
        {
        }

        public AttackRequest(AttackDefinition definition, int startStepIndex, int stepCount)
            : this(definition, startStepIndex, stepCount, false)
        {
        }

        public AttackRequest(AttackDefinition definition, int startStepIndex, int stepCount, bool usePerfectVariant)
        {
            Definition = definition;
            StartStepIndex = startStepIndex;
            StepCount = stepCount;
            UsePerfectVariant = usePerfectVariant;
        }

        public AttackDefinition Definition { get; }
        public int StartStepIndex { get; }
        public int StepCount { get; }
        public bool UsePerfectVariant { get; }
    }
}
