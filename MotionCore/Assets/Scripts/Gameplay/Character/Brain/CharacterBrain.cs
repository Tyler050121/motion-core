using Animancer;
using Animancer.FSM;
using Animancer.Units;
using Cinemachine;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [DisallowMultipleComponent]
    public sealed class CharacterBrain : MonoBehaviour, ICommandReceiver
    {
        [SerializeField] Character m_Character;
        [SerializeField] MoveState m_MoveState;
        [SerializeField] EvadeState m_EvadeState;
        [SerializeField] AttackState m_AttackState;
        [SerializeField] MovementConfig m_MoveConfig;
        [SerializeField, Seconds] float m_InputTimeOut = 0.35f;
        [SerializeField] CinemachineFreeLook m_FreeLookCamera;

        StateMachine<CharacterState>.InputBuffer m_InputBuffer;

        // public float MoveSpeed => m_Character.Parameters.MoveSpeed;
        // public CharacterStateType CurrentStateType => m_Character.StateMachine.CurrentState.Type;

        void Awake()
        {
            ITimerService timer = ServiceLocator.Resolve<ITimerService>();
            m_MoveState.SetContext(timer, m_MoveConfig);
            m_InputBuffer = new StateMachine<CharacterState>.InputBuffer(m_Character.StateMachine);
        }

        void Update()
        {
            m_InputBuffer.Update();
        }

        void LateUpdate()
        {
            if (!m_Character.Parameters.HasFacingDirection)
                return;

            float targetAngle = Mathf.Atan2(
                m_Character.Parameters.FacingDirection.x,
                m_Character.Parameters.FacingDirection.z) * Mathf.Rad2Deg;

            Vector3 eulerAngles = m_Character.FacingRoot.eulerAngles;
            eulerAngles.y = Mathf.MoveTowardsAngle(
                eulerAngles.y,
                targetAngle,
                m_Character.Parameters.FacingTurnSpeed * Time.deltaTime);
            m_Character.FacingRoot.eulerAngles = eulerAngles;
            m_Character.Parameters.ClearFacing();
        }

        public void SetMoveInput(Vector2 moveInput, bool wantsRun)
        {
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            Vector2 clampedInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            float targetSpeed = hasMoveInput ? (wantsRun ? m_MoveConfig.RunSpeed : m_MoveConfig.WalkSpeed) : 0f;
            float speedChangeRate = targetSpeed <= m_MoveConfig.WalkSpeed ? m_MoveConfig.WalkSpeedChangeRate : m_MoveConfig.RunSpeedChangeRate;
            float moveSpeed = Mathf.MoveTowards(m_Character.Parameters.MoveSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

            Quaternion cameraYaw = Quaternion.Euler(0f, m_FreeLookCamera.m_XAxis.Value, 0f);
            Vector3 forward = cameraYaw * Vector3.forward;
            Vector3 right = cameraYaw * Vector3.right;

            Vector3 moveDirection = hasMoveInput
                ? right.normalized * clampedInput.x + forward.normalized * clampedInput.y
                : Vector3.zero;

            m_Character.Parameters.SetMove(clampedInput, moveDirection, moveSpeed, moveSpeed > m_MoveConfig.RunThresholdSpeed);

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

        public bool TryAttack(AttackDefinition definition)
        {
            if (!m_AttackState.QueueAttack(definition))
                return false;

            if (m_Character.StateMachine.CurrentState == m_AttackState)
                return true;

            m_InputBuffer.Buffer(m_AttackState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }
    }
}
