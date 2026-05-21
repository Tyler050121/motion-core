using System;
using Animancer;
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
            [SerializeField, Tooltip("Animancer transition played for this action step")]
            TransitionAsset m_Animation;
            public TransitionAsset Animation => m_Animation;

            [SerializeField] EventBinding[] m_EventBindings = Array.Empty<EventBinding>();
            public EventBinding[] EventBindings => m_EventBindings;

            [SerializeField] AttackEndStepDefinition m_EndStep;
            public AttackEndStepDefinition EndStep => m_EndStep;
            public bool HasEndStep => m_EndStep != null && m_EndStep.Animation != null;

            [SerializeField, Min(0f)] float m_ComboGraceSeconds = 0.35f;
            public float ComboGraceSeconds => m_ComboGraceSeconds;

            [SerializeField] ComboGraceStartType m_ComboGraceStartType = ComboGraceStartType.End;
            public ComboGraceStartType ComboGraceStartType => m_ComboGraceStartType;

            [Serializable]
            public sealed class EventBinding
            {
                [SerializeField] StringAsset m_Event;
                public StringAsset Event => m_Event;

                [SerializeField] EventType m_Type;
                public EventType Type => m_Type;
            }
        }

        [Serializable]
        public sealed class AttackEndStepDefinition
        {
            [SerializeField, Tooltip("Animancer transition played for this end step")]
            TransitionAsset m_Animation;
            public TransitionAsset Animation => m_Animation;

            [SerializeField] AttackStepDefinition.EventBinding[] m_EventBindings = Array.Empty<AttackStepDefinition.EventBinding>();
            public AttackStepDefinition.EventBinding[] EventBindings => m_EventBindings;
        }

        public enum EventType
        {
            CanCancel,
            CanAttack,
            HitStart,
            HitEnd,
            BranchOpen,
            BranchClose,
            Feedback
        }

        public enum ComboGraceStartType
        {
            Start,
            Cancel,
            End
        }
    }
}
