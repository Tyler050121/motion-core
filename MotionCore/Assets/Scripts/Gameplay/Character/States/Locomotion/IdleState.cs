using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class IdleState : CharacterState
    {
        [SerializeField] TransitionAsset m_Idle;

        public override CharacterStateType Type => CharacterStateType.Idle;
        public override CastPriority CurrentCastPriority => CastPriority.None;
        public override StaggerLevel CurrentStaggerLevel => StaggerLevel.None;

        void OnEnable()
        {
            Character.Animancer.Play(m_Idle);
        }
    }
}
