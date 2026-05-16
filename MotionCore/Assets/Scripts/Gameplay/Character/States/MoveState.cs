using Animancer;
using Animancer.FSM;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class MoveState : CharacterState
    {
        [SerializeField] TransitionAsset m_MoveStartMixer;
        [SerializeField] TransitionAsset m_MoveMixer;
        [SerializeField] float m_TurnSpeed = 540f;

        LinearMixerState m_CurrentMixerState;

        public override CharacterStateType Type => CharacterStateType.Move;

        void OnEnable()
        {
            ExitMode = CharacterStateExitMode.CanCancel;
            m_CurrentMixerState = null;

            CharacterState previousState = StateChange<CharacterState>.PreviousState;
            if (previousState.Type == CharacterStateType.Idle)
            {
                m_CurrentMixerState = (LinearMixerState)Character.Animancer.Play(m_MoveStartMixer);
                m_CurrentMixerState.Parameter = Character.Parameters.MoveSpeed;
                m_CurrentMixerState.Events(this).OnEnd = PlayMoveMixer;
                return;
            }

            PlayMoveMixer();
        }

        void Update()
        {
            m_CurrentMixerState.Parameter = Character.Parameters.MoveSpeed;

            Character.Parameters.SetFacing(Character.Parameters.MoveDirection, m_TurnSpeed);
        }

        void PlayMoveMixer()
        {
            m_CurrentMixerState = (LinearMixerState)Character.Animancer.Play(m_MoveMixer);
            m_CurrentMixerState.Parameter = Character.Parameters.MoveSpeed;
        }
    }
}
