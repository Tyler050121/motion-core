using Animancer;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class EvadeState : CharacterState
    {
        [SerializeField] TransitionAsset m_EvadeFront;
        [SerializeField] TransitionAsset m_EvadeBack;

        protected override bool CanInterruptSelf => (ExitOptions & CharacterStateExitOptions.Evade) != 0;
        public override CharacterStateType Type => CharacterStateType.Evade;

        void OnEnable()
        {
            TransitionAsset evade = Character.Parameters.HasMoveInput ? m_EvadeFront : m_EvadeBack;
            ExitOptions = CharacterStateExitOptions.Default;

            AnimancerState state = Character.Animancer.Play(evade);
            bool isNewEventSequence = state.Events(this, out AnimancerEvent.Sequence events);

            if (isNewEventSequence)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (events.GetName(i) == GlobalConfig.AnimationEventNames.CanCancel)
                        events.SetCallback(i, OpenCancel);
                }
            }

            events.OnEnd = ReturnToDefaultState;
        }
    }
}
