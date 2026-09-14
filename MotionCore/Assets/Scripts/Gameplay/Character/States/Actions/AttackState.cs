using System;
using Animancer;
using Animancer.FSM;
using MotionCore.Infrastructure;
using MotionCore.Gameplay.Combat;
using UnityEngine;
using EventNames = MotionCore.GlobalConfig.AnimationEventNames;
using Random = UnityEngine.Random;

namespace MotionCore.Gameplay.Character
{
    public sealed class AttackState : CharacterState
    {
        // 攻击变体固定为 45% 概率触发。
        const float PerfectVariantChance = 0.45f;

        AttackRequest m_PendingRequest;
        AttackDefinition m_CurrentAttack;
        AttackDefinition.AttackStepDefinition m_CurrentStep;
        AttackDefinition m_ComboAttack;
        AttackAnimationTrack m_CurrentTrack;
        CharacterStateType m_ActionType;
        int m_CurrentStepIndex;
        int m_LastStepIndexExclusive;
        int m_ComboStepIndex;
        int m_ComboLastStepIndexExclusive;
        float m_CurrentStepStartTime;
        float m_ComboExpireTime;
        bool m_IsPerfectVariant;
        bool m_IsPlayingEndStep;
        System.Func<Vector3> m_AttackFacingResolver;
        IVfxService m_Vfx;

        [SerializeField] MeleeHitbox m_MeleeHitbox;

        protected override bool CanInterruptSelf => (ExitWindows & CharacterExitWindow.Attack) != 0;
        public override CharacterStateType Type => m_ActionType;
        public override CastPriority CurrentCastPriority => m_CurrentAttack.CastPriority;
        public override CastPriority RequestedCastPriority => m_PendingRequest.Definition != null
            ? m_PendingRequest.Definition.CastPriority
            : CurrentCastPriority;
        public override StaggerLevel CurrentStaggerLevel => m_CurrentAttack.StaggerLevel;

        public void SetAttackFacingResolver(System.Func<Vector3> resolver)
        {
            m_AttackFacingResolver = resolver;
        }

        void Awake()
        {
            m_Vfx = ServiceLocator.Resolve<IVfxService>();
        }

        /// <summary>
        /// 排队一次动作输入。状态未启用时会尝试恢复连段窗口；
        /// 状态播放中会尝试推进到同一动作的下一段或配置的后继攻击。
        /// </summary>
        public void QueueAttack(AttackDefinition definition)
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
                return;
            }

            if (m_CurrentAttack == definition)
            {
                int nextStepIndex = m_CurrentStepIndex + 1;
                if (nextStepIndex >= m_LastStepIndexExclusive)
                    m_PendingRequest = new AttackRequest(definition);
                else
                    m_PendingRequest = new AttackRequest(
                        definition,
                        nextStepIndex,
                        m_LastStepIndexExclusive - nextStepIndex,
                        ShouldUsePerfectVariant(definition, nextStepIndex));
            }
            else if (m_CurrentStepIndex + 1 >= m_LastStepIndexExclusive
                && m_CurrentAttack.ComboFollowUp == definition)
            {
                int followUpStepIndex = m_CurrentAttack.ComboFollowUpStepIndex;
                m_PendingRequest = new AttackRequest(
                    definition,
                    followUpStepIndex,
                    definition.StepCount - followUpStepIndex,
                    ShouldUsePerfectVariant(definition, followUpStepIndex));
            }
            else
                m_PendingRequest = new AttackRequest(definition);

            if (CanInterruptSelf)
                TryConsumePendingRequest();
        }

        void OnEnable()
        {
            PlayAttack(m_PendingRequest);
        }

        void OnDisable()
        {
            bool isReentering = StateChange<CharacterState>.IsActive
                && StateChange<CharacterState>.NextState == this;

            m_MeleeHitbox.CloseAll();
            if (m_ComboAttack == null)
                OpenComboGrace(m_CurrentStep.ComboGraceStartType, true);

            m_CurrentStep = null;
            if (!isReentering)
                m_PendingRequest = default;
            m_LastStepIndexExclusive = 0;
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
            if (!m_CurrentAttack.TryGetStep(m_CurrentStepIndex, out m_CurrentStep))
                throw new InvalidOperationException($"攻击定义 {m_CurrentAttack.name} 的第 {m_CurrentStepIndex} 段未配置。");

            m_PendingRequest = default;
            m_IsPlayingEndStep = false;
            m_CurrentStepStartTime = Time.time;

            Character.Parameters.SetFacing(m_AttackFacingResolver());
            OpenComboGrace(AttackDefinition.ComboGraceStartType.Start);
            PlayStep(m_CurrentStep, attackRequest.UsePerfectVariant);
        }

        void PlayStep(AttackDefinition.AttackStepDefinition step, bool usePerfectVariant)
        {
            if (usePerfectVariant && step.HasPerfectVariant)
            {
                AttackDefinition.AttackStepVariantDefinition variant = step.PerfectVariant;
                m_IsPerfectVariant = true;
                PlayTrack(variant.Track);
                return;
            }

            m_IsPerfectVariant = false;
            PlayTrack(step.Track);
        }

        void PlayTrack(AttackAnimationTrack track)
        {
            m_MeleeHitbox.CloseAll();

            // 事件回调只绑定一次、之后重播复用，所以命中数据只能从字段读、不能被回调捕获。
            m_CurrentTrack = track;
            PlayWithEvents(track.Animation, OnStepEnded);

            // 收招轨道不再承担攻击判定，允许立刻回到移动或衔接其他主动动作。
            if (m_IsPlayingEndStep)
                ExitWindows = CharacterExitWindow.Move | CharacterExitWindow.Attack;
        }

        protected override void BindEvent(AnimancerEvent.Sequence events, int index, string name)
        {
            if (name == EventNames.CanCancel)
                Bind(events, index, OpenStepCancel);
            else if (name == EventNames.CanAttack)
                Bind(events, index, OpenStepAttack);
            else if (name == EventNames.Hit)
                Bind(events, index, Hit);
            else if (name == EventNames.HitStart)
                Bind(events, index, OpenHit);
            else if (name == EventNames.HitEnd)
                Bind(events, index, m_MeleeHitbox.Close);
            else
                base.BindEvent(events, index, name);
        }

        void Hit(int index)
        {
            AttackHitDefinition hit = m_CurrentTrack.GetHitDefinition(index);
            Transform source = Character.GetAnchor(hit.Anchor);
            Transform rayOrigin = Character.GetAnchor(CharacterAnchor.CameraTarget);
            m_MeleeHitbox.Hit(index, hit.Profile, source, hit.LocalOffset, rayOrigin);
            PlayVfx(source, hit.Vfx);
        }

        void OpenHit(int index)
        {
            AttackHitDefinition hit = m_CurrentTrack.GetHitDefinition(index);
            Transform source = Character.GetAnchor(hit.Anchor);
            Transform rayOrigin = Character.GetAnchor(CharacterAnchor.CameraTarget);
            m_MeleeHitbox.Open(index, hit.Profile, source, hit.LocalOffset, rayOrigin);
            PlayVfx(source, hit.Vfx);
        }

        void PlayVfx(Transform source, AttackVfxDefinition definition)
        {
            if (!definition.IsValid)
                return;

            Transform spawnSource = definition.SpawnPoint == AttackVfxSpawnPoint.Anchor
                ? source
                : Character.FacingRoot;

            Vector3 worldPosition = spawnSource.TransformPoint(definition.LocalOffset);
            Quaternion worldRotation = Character.FacingRoot.rotation * Quaternion.Euler(definition.LocalEulerAngles);
            m_Vfx.Play(definition.Preset, new VfxSpawnRequest(
                worldPosition,
                worldRotation,
                definition.Scale,
                definition.Speed,
                spawnSource,
                definition.FollowMode));
        }

        /// <summary>
        /// 打开当前段的取消窗口。如果之前已经缓存了下一段输入，
        /// 这里会立即消费并切到下一段。
        /// </summary>
        void OpenStepCancel()
        {
            OpenCancel();

            if ((ExitWindows & CharacterExitWindow.Attack) == 0)
                return;

            TryConsumePendingRequest();
        }

        void OpenStepAttack()
        {
            OpenAttack();
            TryConsumePendingRequest();
        }

        void TryConsumePendingRequest()
        {
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
                    PlayTrack(m_CurrentStep.PerfectVariant.EndStep);
                    return;
                }

                if (m_CurrentStep.HasEndStep)
                {
                    m_IsPlayingEndStep = true;
                    PlayTrack(m_CurrentStep.EndStep);
                    return;
                }
            }

            m_MeleeHitbox.CloseAll();
            m_PendingRequest = default;
            ReturnToDefaultState();
        }

        #endregion

        #region 攻击变体概率

        bool ShouldUsePerfectVariant(AttackDefinition definition, int stepIndex)
        {
            return definition.TryGetStep(stepIndex, out AttackDefinition.AttackStepDefinition step)
                && step.HasPerfectVariant
                && Random.value < PerfectVariantChance;
        }

        #endregion

        #region 连段窗口

        void OpenComboGrace(AttackDefinition.ComboGraceStartType startType, bool force = false)
        {
            if (!force && m_CurrentStep.ComboGraceStartType != startType)
                return;

            AttackDefinition comboAttack = m_CurrentAttack;
            int nextStepIndex = m_CurrentStepIndex + 1;
            int lastStepIndexExclusive = m_LastStepIndexExclusive;
            if (nextStepIndex >= lastStepIndexExclusive)
            {
                comboAttack = m_CurrentAttack.ComboFollowUp;
                if (comboAttack == null)
                    return;

                nextStepIndex = m_CurrentAttack.ComboFollowUpStepIndex;
                lastStepIndexExclusive = comboAttack.StepCount;
            }

            m_ComboAttack = comboAttack;
            m_ComboStepIndex = nextStepIndex;
            m_ComboLastStepIndexExclusive = lastStepIndexExclusive;
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
                    ? m_CurrentStep.PerfectVariant.Track.Animation
                    : m_CurrentStep.Track.Animation;
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

        #endregion
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
