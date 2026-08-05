using System;
using MotionCore.Gameplay.Character;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "MotionCore/Combat/Attack Definition")]
    public sealed class AttackDefinition : ScriptableObject
    {
        [SerializeField] CharacterStateType m_StateType = CharacterStateType.BasicAttack;
        public CharacterStateType StateType => m_StateType;

        [SerializeField, Tooltip("施法优先级")] CastPriority m_CastPriority = CastPriority.BasicAttack;
        public CastPriority CastPriority => m_CastPriority;

        [SerializeField, Tooltip("人物状态等级")] StaggerLevel m_StaggerLevel = StaggerLevel.LightAttack;
        public StaggerLevel StaggerLevel => m_StaggerLevel;

        [SerializeField] AttackStepDefinition[] m_Steps = Array.Empty<AttackStepDefinition>();
        public AttackStepDefinition[] Steps => m_Steps;
        public int StepCount => m_Steps.Length;

        [SerializeField, Tooltip("当前攻击结束后可衔接的攻击")]
        AttackDefinition m_ComboFollowUp;
        public AttackDefinition ComboFollowUp => m_ComboFollowUp;

        [SerializeField, Min(0), Tooltip("后继攻击起始段索引，0 表示第一段")]
        int m_ComboFollowUpStepIndex;
        public int ComboFollowUpStepIndex => m_ComboFollowUpStepIndex;

        public bool TryGetStep(int stepIndex, out AttackStepDefinition step)
        {
            if (stepIndex >= 0 && stepIndex < m_Steps.Length)
            {
                step = m_Steps[stepIndex];
                return step != null;
            }

            step = null;
            return false;
        }

        [Serializable]
        public sealed class AttackStepDefinition
        {
            [SerializeField] AttackAnimationTrack m_Track;
            public AttackAnimationTrack Track => m_Track;

            [SerializeField] AttackAnimationTrack m_EndStep;
            public AttackAnimationTrack EndStep => m_EndStep;
            public bool HasEndStep => m_EndStep != null;

            [SerializeField] AttackStepVariantDefinition m_PerfectVariant;
            public AttackStepVariantDefinition PerfectVariant => m_PerfectVariant;
            public bool HasPerfectVariant => m_PerfectVariant != null && m_PerfectVariant.Track != null;

            [SerializeField, Min(0f)] float m_ComboGraceSeconds = 0.35f;
            public float ComboGraceSeconds => m_ComboGraceSeconds;

            [SerializeField] ComboGraceStartType m_ComboGraceStartType = ComboGraceStartType.End;
            public ComboGraceStartType ComboGraceStartType => m_ComboGraceStartType;
        }

        [Serializable]
        public sealed class AttackStepVariantDefinition
        {
            [SerializeField] AttackAnimationTrack m_Track;
            public AttackAnimationTrack Track => m_Track;

            [SerializeField] AttackAnimationTrack m_EndStep;
            public AttackAnimationTrack EndStep => m_EndStep;
            public bool HasEndStep => m_EndStep != null;
        }

        public enum ComboGraceStartType
        {
            Start = 0, // 出手时
            Cancel = 1, // 取消窗口时
            End = 2, // 动画结束时
        }
    }
}
