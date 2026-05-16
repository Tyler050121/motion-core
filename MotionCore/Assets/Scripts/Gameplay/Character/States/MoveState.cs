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

            Vector2 move = Character.Parameters.MoveInput;
            if (move.sqrMagnitude <= 0.0001f)
                return;

            float targetAngle = Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg;
            Vector3 eulerAngles = Character.FacingRoot.eulerAngles;
            eulerAngles.y = Mathf.MoveTowardsAngle(eulerAngles.y, targetAngle, m_TurnSpeed * Time.deltaTime);
            Character.FacingRoot.eulerAngles = eulerAngles;
        }

        void PlayMoveMixer()
        {
            m_CurrentMixerState = (LinearMixerState)Character.Animancer.Play(m_MoveMixer);
            m_CurrentMixerState.Parameter = Character.Parameters.MoveSpeed;
        }
    }
}
