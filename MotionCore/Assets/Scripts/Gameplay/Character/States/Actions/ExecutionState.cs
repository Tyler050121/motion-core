using Animancer;
using MotionCore.Gameplay.Combat;
using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Targeting;
using UnityEngine;
using EventNames = MotionCore.GlobalConfig.AnimationEventNames;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 对齐破韧目标、播放处决动作并在动画事件处结算伤害。
    /// </summary>
    public sealed class ExecutionState : CharacterState
    {
        [SerializeField, Tooltip("处决动作定义")] AttackDefinition m_Execution;
        [SerializeField] Hurtbox m_Hurtbox;
        [SerializeField, Min(0.01f), Tooltip("处决最大距离")] float m_MaxDistance = 3f;
        [SerializeField, Range(1f, 180f), Tooltip("处决最大朝向夹角")] float m_MaxFacingAngle = 75f;
        [SerializeField, Range(0f, 1f), Tooltip("处决目标评分中的朝向权重，剩余权重用于距离")]
        float m_FacingScoreWeight = 0.65f;

        PreparedExecutionTarget m_PreparedTarget;

        public override CharacterStateType Type => m_Execution.StateType;
        public override CastPriority CurrentCastPriority => m_Execution.CastPriority;
        public override StaggerLevel CurrentStaggerLevel => m_Execution.StaggerLevel;
        public override bool CanEnterState
            => m_PreparedTarget.IsValid && m_PreparedTarget.ExecutionTarget.CanBeExecuted;
        AttackAnimationTrack ExecutionTrack => m_Execution.Steps[0].Track;

        void OnEnable()
        {
            m_PreparedTarget.ExecutionTarget.TryClaim();
            Character.Parameters.SetFacing(m_PreparedTarget.FacingDirection, 0f);
            m_Hurtbox.SetInvulnerable(true);
            PlayWithEvents(ExecutionTrack.Animation, ReturnToDefaultState);
        }

        void OnDisable()
        {
            m_Hurtbox.SetInvulnerable(false);
            m_PreparedTarget = default;
        }

        /// <summary>
        /// 按朝向和距离评分查找最佳可处决目标，并缓存进入状态后的目标朝向。
        /// </summary>
        public bool TryPrepare()
        {
            m_PreparedTarget = default;
            float bestScore = float.PositiveInfinity;

            foreach (LockOnTarget target in LockOnTarget.Targets)
            {
                if (target.transform.root == Character.transform.root || !target.IsAvailable)
                    continue;

                Character targetCharacter = target.GetComponentInParent<Character>();
                // LockOnTarget 也用于不可处决的场景物体，例如训练假人。
                if (targetCharacter == null)
                    continue;

                IExecutionTarget targetExecution = targetCharacter.GetComponentInChildren<IExecutionTarget>(true);

                // 目标处于可处决窗口内
                if (!targetExecution.CanBeExecuted)
                    continue;
                if (!targetCharacter.TryGetAnchor(CharacterAnchor.Execution, out Transform executionAnchor))
                    continue;

                // 距离
                Vector3 toAnchor = executionAnchor.position - Character.transform.position;
                toAnchor.y = 0f;
                float distance = toAnchor.magnitude;
                if (distance > m_MaxDistance)
                    continue;

                // 朝向
                Vector3 facingDirection = targetCharacter.FacingRoot.position - Character.FacingRoot.position;
                facingDirection.y = 0f;
                float facingAngle = Vector3.Angle(Character.FacingRoot.forward, facingDirection);
                if (facingAngle > m_MaxFacingAngle)
                    continue;

                // 评分
                float score = facingAngle / m_MaxFacingAngle * m_FacingScoreWeight
                    + distance / m_MaxDistance * (1f - m_FacingScoreWeight);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                m_PreparedTarget = new PreparedExecutionTarget(target.Health, targetExecution, facingDirection);
            }

            return m_PreparedTarget.IsValid;
        }

        protected override void BindEvent(AnimancerEvent.Sequence events, int index, string name)
        {
            if (name == EventNames.Hit)
                Bind(events, index, ApplyExecutionHit);
            else
                base.BindEvent(events, index, name);
        }

        void ApplyExecutionHit(int index)
        {
            AttackHitDefinition hit = ExecutionTrack.GetHitDefinition(index);
            m_PreparedTarget.Health.ApplyDamage(hit.Profile.Damage);
        }

        readonly struct PreparedExecutionTarget
        {
            public PreparedExecutionTarget(Health health, IExecutionTarget executionTarget,
                Vector3 facingDirection)
            {
                Health = health;
                ExecutionTarget = executionTarget;
                FacingDirection = facingDirection;
            }

            public Health Health { get; }
            public IExecutionTarget ExecutionTarget { get; }
            public Vector3 FacingDirection { get; }
            public bool IsValid => ExecutionTarget != null;
        }
    }
}
