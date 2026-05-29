using System;
using MotionCore.Gameplay.Combat;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [CreateAssetMenu(menuName = "MotionCore/Character/Attack Definition")]
    public sealed class AttackDefinition : ScriptableObject
    {
        [SerializeField] CharacterStateType m_StateType = CharacterStateType.BasicAttack;
        public CharacterStateType StateType => m_StateType;

        [SerializeField] AttackStepDefinition[] m_Steps = Array.Empty<AttackStepDefinition>();
        public AttackStepDefinition[] Steps => m_Steps;
        public int StepCount => m_Steps.Length;

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
            [SerializeField] AttackAnimTrackAsset m_Track;
            public AttackAnimTrackAsset Track => m_Track;

            [SerializeField] AttackAnimTrackAsset m_EndStep;
            public AttackAnimTrackAsset EndStep => m_EndStep;
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
            [SerializeField] AttackAnimTrackAsset m_Track;
            public AttackAnimTrackAsset Track => m_Track;

            [SerializeField] AttackAnimTrackAsset m_EndStep;
            public AttackAnimTrackAsset EndStep => m_EndStep;
            public bool HasEndStep => m_EndStep != null;
        }

        public enum ComboGraceStartType
        {
            Start,
            Cancel,
            End
        }
    }
}
