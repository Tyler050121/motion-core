using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class IdleState : CharacterState
    {
        [SerializeField] TransitionAsset m_Idle;

        public override CharacterStateType Type => CharacterStateType.Idle;

        void OnEnable()
        {
            ExitOptions = CharacterStateExitOptions.AllActions;
            Character.Animancer.Play(m_Idle);
        }
    }
}
