using Animancer;
using Animancer.FSM;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public enum CharacterStateType
    {
        Idle,
        Move,
        Evade,
        TurnBack,
        SwitchIn,
        SwitchOut,
        QuestStart,
        BasicAttack,
        HeavyAttack,
        Skill,
        Ultimate,
        Hit,
        Dead
    }

    [System.Flags]
    public enum CharacterStateExitOptions
    {
        None = 0,
        Move = 1 << 0,
        Evade = 1 << 1,
        Attack = 1 << 2,
        Skill = 1 << 3,
        Switch = 1 << 4,
        Idle = 1 << 5,
        Default = Evade,
        Cancel = Move | Skill | Switch | Attack,
        All = Default | Cancel
    }

    public abstract class CharacterState : StateBehaviour
    {
        [SerializeField] Character m_Character;

        protected Character Character => m_Character;
        protected CharacterStateExitOptions ExitOptions { get; set; } = CharacterStateExitOptions.All;
        protected virtual bool CanInterruptSelf => false;

        public abstract CharacterStateType Type { get; }

        protected void OpenCanCancel() => ExitOptions |= CharacterStateExitOptions.Cancel;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            gameObject.GetComponentInParentOrChildren(ref m_Character);
        }
#endif

        public override bool CanExitState
        {
            get
            {
                CharacterState nextState = m_Character.StateMachine.NextState;
                if (nextState == this)
                    return CanInterruptSelf;

                return CharacterStateRules.CanExit(Type, nextState.Type, ExitOptions);
            }
        }
    }
}
