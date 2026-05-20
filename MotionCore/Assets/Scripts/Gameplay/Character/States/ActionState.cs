using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class ActionState : CharacterState
    {
        ActionRequest m_PendingRequest;
        ActionDefinition m_CurrentAction;
        ActionDefinition.ActionStepDefinition m_CurrentStep;
        ActionDefinition m_ComboAction;
        CharacterStateType m_ActionType;
        int m_CurrentStepIndex;
        int m_LastStepIndexExclusive;
        int m_ComboStepIndex;
        int m_ComboLastStepIndexExclusive;
        float m_CurrentStepStartTime;
        float m_ComboExpireTime;
        bool m_IsPlayingEndStep;

        protected override bool CanInterruptSelf => ExitPhase == CharacterStateExitPhase.CanCancel;
        public override CharacterStateType Type => m_ActionType;

        /// <summary>
        /// 排队一次动作输入。状态未启用时会尝试恢复连段窗口；
        /// 状态播放中且是同一个动作时，会尝试推进到下一段。
        /// </summary>
        public bool QueueAction(ActionDefinition definition)
        {
            if (!enabled)
            {
                if (CanResumeCombo(definition))
                {
                    PlayAction(new ActionRequest(
                        definition,
                        m_ComboStepIndex,
                        m_ComboLastStepIndexExclusive - m_ComboStepIndex));
                }
                else
                    PlayAction(new ActionRequest(definition));

                return true;
            }

            if (m_CurrentAction == definition)
            {
                int nextStepIndex = m_CurrentStepIndex + 1;
                if (nextStepIndex >= m_LastStepIndexExclusive)
                {
                    ActionRequest restartRequest = new(definition);
                    if (ExitPhase == CharacterStateExitPhase.CanCancel)
                        PlayAction(restartRequest);
                    else
                        m_PendingRequest = restartRequest;

                    return true;
                }

                ActionRequest nextRequest = new(
                    definition,
                    nextStepIndex,
                    m_LastStepIndexExclusive - nextStepIndex);

                if (ExitPhase == CharacterStateExitPhase.CanCancel)
                    PlayAction(nextRequest);
                else
                    m_PendingRequest = nextRequest;

                return true;
            }

            return false;
        }

        void OnDisable()
        {
            if (m_ComboAction == null)
                OpenComboGrace(m_CurrentStep.ComboGraceStartType, true);

            m_CurrentAction = null;
            m_CurrentStep = null;
            m_PendingRequest = default;
            m_LastStepIndexExclusive = 0;
            m_IsPlayingEndStep = false;
        }

        #region 播放流程
        void PlayAction(ActionRequest actionRequest)
        {
            m_CurrentAction = actionRequest.Definition;
            ClearCombo();
            m_ActionType = m_CurrentAction.StateType;
            m_CurrentStepIndex = actionRequest.StartStepIndex;
            m_LastStepIndexExclusive = actionRequest.StepCount < 0
                ? m_CurrentAction.StepCount
                : Mathf.Min(m_CurrentStepIndex + actionRequest.StepCount, m_CurrentAction.StepCount);
            m_CurrentAction.TryGetStep(m_CurrentStepIndex, out m_CurrentStep);
            m_PendingRequest = default;
            m_IsPlayingEndStep = false;
            m_CurrentStepStartTime = Time.time;

            OpenComboGrace(ActionDefinition.ComboGraceStartType.Start);
            PlayStep(m_CurrentStep);
        }

        void PlayStep(ActionDefinition.ActionStepDefinition step)
        {
            PlayStep(step.Animation, step.EventBindings);
        }

        void PlayStep(ActionDefinition.ActionEndStepDefinition step)
        {
            PlayStep(step.Animation, step.EventBindings);
        }

        void PlayStep(TransitionAsset animation, ActionDefinition.ActionStepDefinition.EventBinding[] eventBindings)
        {
            ExitPhase = CharacterStateExitPhase.CanCancel;
            AnimancerState state = Character.Animancer.Play(animation);
            AnimancerEvent.Sequence events = state.Events(this);

            foreach (ActionDefinition.ActionStepDefinition.EventBinding binding in eventBindings)
            {
                ActionDefinition.EventType eventType = binding.Type;
                if (eventType == ActionDefinition.EventType.CanCancel)
                {
                    ExitPhase = CharacterStateExitPhase.Locked;
                    events.SetCallback(binding.Event, OpenActionCancel);
                }
            }

            events.OnEnd = OnStepEnded;
        }

        /// <summary>
        /// 打开当前段的取消窗口。如果之前已经缓存了下一段输入，
        /// 这里会立即消费并切到下一段。
        /// </summary>
        void OpenActionCancel()
        {
            ExitPhase = CharacterStateExitPhase.CanCancel;

            if (m_PendingRequest.Definition != null)
                PlayAction(m_PendingRequest);
        }

        void OnStepEnded()
        {
            if (!m_IsPlayingEndStep)
            {
                OpenComboGrace(ActionDefinition.ComboGraceStartType.End);

                if (m_CurrentStep.HasEndStep)
                {
                    m_IsPlayingEndStep = true;
                    PlayStep(m_CurrentStep.EndStep);
                    return;
                }
            }

            m_PendingRequest = default;
            ExitPhase = CharacterStateExitPhase.Finished;
            Character.StateMachine.TrySetDefaultState();
        }

        #endregion

        #region 连段窗口

        void OpenComboGrace(ActionDefinition.ComboGraceStartType startType, bool force = false)
        {
            if (!force && m_CurrentStep.ComboGraceStartType != startType)
                return;

            int nextStepIndex = m_CurrentStepIndex + 1;
            if (nextStepIndex >= m_LastStepIndexExclusive)
                return;

            m_ComboAction = m_CurrentAction;
            m_ComboStepIndex = nextStepIndex;
            m_ComboLastStepIndexExclusive = m_LastStepIndexExclusive;
            m_ComboExpireTime = GetComboGraceStartTime(force) + m_CurrentStep.ComboGraceSeconds;
        }

        bool CanResumeCombo(ActionDefinition definition)
        {
            return m_ComboAction == definition
                && Time.time <= m_ComboExpireTime
                && m_ComboStepIndex < m_ComboLastStepIndexExclusive;
        }

        float GetComboGraceStartTime(bool force)
        {
            if (!force)
                return Time.time;

            if (m_CurrentStep.ComboGraceStartType == ActionDefinition.ComboGraceStartType.End)
            {
                ITransition transition = m_CurrentStep.Animation.GetTransition();
                float speed = Mathf.Abs(transition.Speed);
                float duration = speed > 0f ? transition.MaximumLength / speed : transition.MaximumLength;
                return m_CurrentStepStartTime + duration;
            }

            return Time.time;
        }

        void ClearCombo()
        {
            m_ComboAction = null;
            m_ComboStepIndex = 0;
            m_ComboLastStepIndexExclusive = 0;
            m_ComboExpireTime = 0f;
        }

        #endregion
    }

    public readonly struct ActionRequest
    {
        public ActionRequest(ActionDefinition definition)
            : this(definition, 0, -1)
        {
        }

        public ActionRequest(ActionDefinition definition, int startStepIndex, int stepCount)
        {
            Definition = definition;
            StartStepIndex = startStepIndex;
            StepCount = stepCount;
        }

        public ActionDefinition Definition { get; }
        public int StartStepIndex { get; }
        public int StepCount { get; }
    }
}
