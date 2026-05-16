using Animancer;
using Animancer.FSM;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public abstract class CharacterState : StateBehaviour
    {
        [SerializeField] Character m_Character;

        protected Character Character => m_Character;
        protected CharacterStateExitMode ExitMode { get; set; } = CharacterStateExitMode.CanCancel;
        protected virtual bool CanInterruptSelf => false;

        public abstract CharacterStateType Type { get; }

        protected void OpenCanCancel() => ExitMode = CharacterStateExitMode.CanCancel;

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

                return CharacterStateRules.CanExit(Type, nextState.Type, ExitMode);
            }
        }
    }
}
