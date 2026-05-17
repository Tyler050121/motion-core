using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class EvadeState : CharacterState
    {
        [SerializeField] TransitionAsset m_EvadeFront;
        [SerializeField] TransitionAsset m_EvadeBack;
        [SerializeField] StringAsset m_CanCancelEvent;

        protected override bool CanInterruptSelf => ExitPhase == CharacterStateExitPhase.CanCancel;
        public override CharacterStateType Type => CharacterStateType.Evade;

        void OnEnable()
        {
            TransitionAsset evade = Character.Parameters.HasMoveInput ? m_EvadeFront : m_EvadeBack;
            ExitPhase = CharacterStateExitPhase.Locked;

            AnimancerState state = Character.Animancer.Play(evade);
            AnimancerEvent.Sequence events = state.Events(this);
            events.SetCallback(m_CanCancelEvent, OpenCanCancel);
            events.OnEnd = () => { ExitPhase = CharacterStateExitPhase.Finished; Character.StateMachine.TrySetDefaultState(); };
        }
    }
}
