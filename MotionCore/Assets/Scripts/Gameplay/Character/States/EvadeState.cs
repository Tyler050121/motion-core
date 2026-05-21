using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class EvadeState : CharacterState
    {
        [SerializeField] TransitionAsset m_EvadeFront;
        [SerializeField] TransitionAsset m_EvadeBack;
        [SerializeField] StringAsset m_CanCancelEvent;

        protected override bool CanInterruptSelf => (ExitOptions & CharacterStateExitOptions.Evade) != 0;
        public override CharacterStateType Type => CharacterStateType.Evade;

        void OnEnable()
        {
            TransitionAsset evade = Character.Parameters.HasMoveInput ? m_EvadeFront : m_EvadeBack;
            ExitOptions = CharacterStateExitOptions.Default;

            AnimancerState state = Character.Animancer.Play(evade);
            AnimancerEvent.Sequence events = state.Events(this);
            events.SetCallback(m_CanCancelEvent, OpenCanCancel);
            events.OnEnd = () => { ExitOptions |= CharacterStateExitOptions.Idle; Character.StateMachine.TrySetDefaultState(); };
        }
    }
}
