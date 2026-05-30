using Animancer;
using Animancer.FSM;
using Animancer.Units;
using MotionCore.Gameplay.Cameras;
using MotionCore.Gameplay.Targeting;
using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [DisallowMultipleComponent]
    public sealed class CharacterBrain : MonoBehaviour, ICommandReceiver, IConfigReceiver<CharacterDefinition>
    {
        [SerializeField] Character m_Character;
        [SerializeField] MoveState m_MoveState;
        [SerializeField] EvadeState m_EvadeState;
        [SerializeField] AttackState m_AttackState;
        [SerializeField] TargetLockController m_TargetLockController;
        [SerializeField, Seconds] float m_InputTimeOut = 0.35f;

        StateMachine<CharacterState>.InputBuffer m_InputBuffer;
        ICameraService m_Camera;
        MotorConfig m_MotorConfig;
        AttackDefinition m_NormalAttack;

        void Awake()
        {
            m_AttackState.SetAttackFacingResolver(GetAttackFacingDirection);
            m_InputBuffer = new StateMachine<CharacterState>.InputBuffer(m_Character.StateMachine);
        }

        public void Initialize(CharacterDefinition definition)
        {
            m_Camera = ServiceLocator.Resolve<ICameraService>();
            m_MotorConfig = definition.Motor;
            m_NormalAttack = definition.BasicAttack;
            ITimerService timer = ServiceLocator.Resolve<ITimerService>();
            m_MoveState.SetContext(timer, m_MotorConfig);
        }

        void Update()
        {
            m_InputBuffer.Update();
        }

        void LateUpdate()
        {
            if (!m_Character.Parameters.HasFacingDirection)
                return;

            TurnFacingToward(m_Character.Parameters.FacingDirection);
        }

        public void SetMoveInput(Vector2 moveInput, bool wantsRun)
        {
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            Vector2 clampedInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            float targetSpeed = hasMoveInput ? (wantsRun ? m_MotorConfig.RunSpeed : m_MotorConfig.WalkSpeed) : 0f;
            float speedChangeRate = targetSpeed <= m_MotorConfig.WalkSpeed ? m_MotorConfig.WalkSpeedChangeRate : m_MotorConfig.RunSpeedChangeRate;
            float moveSpeed = Mathf.MoveTowards(m_Character.Parameters.MoveSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

            Vector3 forward = m_Camera.PlanarForward;
            Vector3 right = m_Camera.PlanarRight;

            Vector3 moveDirection = hasMoveInput
                ? right.normalized * clampedInput.x + forward.normalized * clampedInput.y
                : Vector3.zero;

            m_Character.Parameters.SetMove(clampedInput, moveDirection, moveSpeed, moveSpeed > m_MotorConfig.RunThresholdSpeed);

            if (hasMoveInput)
                m_Character.StateMachine.TrySetState(m_MoveState);
            else
                m_Character.StateMachine.TrySetDefaultState();
        }

        public bool TryEvade()
        {
            m_InputBuffer.Buffer(m_EvadeState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }

        public bool TryNormalAttack()
        {
            return TryAttack(m_NormalAttack);
        }

        public void ToggleTargetLock()
        {
            m_TargetLockController.ToggleLock();
        }

        bool TryAttack(AttackDefinition definition)
        {
            if (!m_AttackState.QueueAttack(definition))
                return false;

            if (m_Character.StateMachine.CurrentState == m_AttackState)
                return true;

            m_InputBuffer.Buffer(m_AttackState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }

        Vector3 GetAttackFacingDirection()
        {
            return m_TargetLockController.HasTarget
                ? m_TargetLockController.GetDirectionFrom(m_Character.FacingRoot.position)
                : m_Camera.PlanarForward;
        }

        void TurnFacingToward(Vector3 facingDirection)
        {
            facingDirection.y = 0f;
            Transform facingRoot = m_Character.FacingRoot;
            Quaternion targetRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);

            if (m_MotorConfig.FacingTurnDuration <= 0f)
            {
                facingRoot.rotation = targetRotation;
                m_Character.Parameters.ClearFacing();
                return;
            }

            float maxDegreesDelta = 180f / m_MotorConfig.FacingTurnDuration * Time.deltaTime;
            facingRoot.rotation = Quaternion.RotateTowards(facingRoot.rotation, targetRotation, maxDegreesDelta);

            if (Quaternion.Angle(facingRoot.rotation, targetRotation) <= 0.1f)
                m_Character.Parameters.ClearFacing();
        }
    }
}
