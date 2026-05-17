using Animancer;
using Animancer.FSM;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class MoveState : CharacterState
    {
        [SerializeField] TransitionAsset m_MoveStartMixer;
        [SerializeField] TransitionAsset m_MoveMixer;
        [SerializeField] TransitionAsset m_MoveEndMixer;
        [SerializeField] TransitionAsset m_RunTurnBack;
        [SerializeField] float m_RunTurnBackAngle = 135f;
        [SerializeField] float m_TurnSpeed = 540f;

        LinearMixerState m_CurrentMixerState;
        bool m_IsExitingToIdle;
        bool m_IsTurningBack;

        public override CharacterStateType Type => CharacterStateType.Move;

        public override bool CanExitState
        {
            get
            {
                CharacterState nextState = Character.StateMachine.NextState;
                if (!m_IsExitingToIdle && nextState.Type == CharacterStateType.Idle)
                    PlayMoveEndMixer();

                return base.CanExitState;
            }
        }

        void OnEnable()
        {
            ExitPhase = CharacterStateExitPhase.CanCancel;
            m_IsExitingToIdle = false;
            m_IsTurningBack = false;
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
            if (m_IsExitingToIdle && Character.Parameters.HasMoveInput)
            {
                m_IsExitingToIdle = false;
                PlayMoveMixer();
            }

            if (ShouldPlayRunTurnBack())
            {
                PlayRunTurnBack();
                return;
            }

            if (m_IsTurningBack)
            {
                Character.Parameters.SetFacing(Character.Parameters.MoveDirection, m_TurnSpeed);
                return;
            }

            m_CurrentMixerState.Parameter = Character.Parameters.MoveSpeed;

            Character.Parameters.SetFacing(Character.Parameters.MoveDirection, m_TurnSpeed);
        }

        void PlayMoveMixer()
        {
            m_IsTurningBack = false;
            m_CurrentMixerState = (LinearMixerState)Character.Animancer.Play(m_MoveMixer);
            m_CurrentMixerState.Parameter = Character.Parameters.MoveSpeed;
        }

        void PlayMoveEndMixer()
        {
            m_IsExitingToIdle = true;
            m_IsTurningBack = false;

            //* 目前这个Mixer只有跑步的结束动画，所以如果不是跑步了就直接切Idle。
            if (!Character.Parameters.IsRunning)
            {
                ExitPhase = CharacterStateExitPhase.Finished;
                return;
            }

            m_CurrentMixerState = (LinearMixerState)Character.Animancer.Play(m_MoveEndMixer);
            m_CurrentMixerState.Parameter = Character.Parameters.MoveSpeed;
            m_CurrentMixerState.Events(this).OnEnd = () =>
            {
                ExitPhase = CharacterStateExitPhase.Finished;
                Character.StateMachine.TrySetDefaultState();
            };
        }

        bool ShouldPlayRunTurnBack()
        {
            if (m_IsTurningBack)
                return false;

            if (!Character.Parameters.IsRunning || !Character.Parameters.HasMoveInput)
                return false;

            Vector3 moveDirection = Character.Parameters.MoveDirection;
            moveDirection.y = 0f;

            Vector3 facingDirection = Character.FacingRoot.forward;
            facingDirection.y = 0f;

            float angle = Vector3.Angle(facingDirection, moveDirection);
            return angle >= m_RunTurnBackAngle;
        }

        void PlayRunTurnBack()
        {
            m_IsTurningBack = true;
            AnimancerState state = Character.Animancer.Play(m_RunTurnBack);
            state.Events(this).OnEnd = PlayMoveMixer;
        }
    }
}
