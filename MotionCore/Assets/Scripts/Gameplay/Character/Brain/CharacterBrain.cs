using Animancer;
using Animancer.FSM;
using Animancer.Units;
using Cinemachine;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [DisallowMultipleComponent]
    public sealed class CharacterBrain : MonoBehaviour, ICharacterActionPlayer
    {
        [SerializeField] Character m_Character;
        [SerializeField] MoveState m_MoveState;
        [SerializeField] EvadeState m_EvadeState;
        [SerializeField, Seconds] float m_InputTimeOut = 0.35f;
        [SerializeField] float m_WalkSpeed = 1f;
        [SerializeField] float m_RunSpeed = 3f;
        [SerializeField] float m_WalkSpeedChangeRate = 8f;
        [SerializeField] float m_RunSpeedChangeRate = 3f;
        [SerializeField] CinemachineFreeLook m_FreeLookCamera;

        float m_RunThresholdSpeed;
        StateMachine<CharacterState>.InputBuffer m_InputBuffer;

        public float MoveSpeed => m_Character.Parameters.MoveSpeed;
        // public CharacterStateType CurrentStateType => m_Character.StateMachine.CurrentState.Type;

        void Awake()
        {
            m_RunThresholdSpeed = (m_WalkSpeed + m_RunSpeed) * 0.5f;
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
            float targetSpeed = hasMoveInput ? (wantsRun ? m_RunSpeed : m_WalkSpeed) : 0f;
            float speedChangeRate = targetSpeed <= m_WalkSpeed ? m_WalkSpeedChangeRate : m_RunSpeedChangeRate;
            float moveSpeed = Mathf.MoveTowards(m_Character.Parameters.MoveSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

            Quaternion cameraYaw = Quaternion.Euler(0f, m_FreeLookCamera.m_XAxis.Value, 0f);
            Vector3 forward = cameraYaw * Vector3.forward;
            Vector3 right = cameraYaw * Vector3.right;

            Vector3 moveDirection = hasMoveInput
                ? right.normalized * clampedInput.x + forward.normalized * clampedInput.y
                : Vector3.zero;

            m_Character.Parameters.SetMove(clampedInput, moveDirection, moveSpeed, m_RunThresholdSpeed);

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
    }
}
