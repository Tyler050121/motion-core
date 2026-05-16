using Animancer;
using Animancer.FSM;
using Animancer.Units;
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

        StateMachine<CharacterState>.InputBuffer m_InputBuffer;

        public float MoveSpeed => m_Character.Parameters.MoveSpeed;

        void Awake()
        {
            m_InputBuffer = new StateMachine<CharacterState>.InputBuffer(m_Character.StateMachine);
        }

        void Update()
        {
            m_InputBuffer.Update();
        }

        public void SetMoveInput(Vector2 moveInput, bool wantsRun)
        {
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            float targetSpeed = hasMoveInput ? (wantsRun ? m_RunSpeed : m_WalkSpeed) : 0f;
            float speedChangeRate = targetSpeed <= m_WalkSpeed ? m_WalkSpeedChangeRate : m_RunSpeedChangeRate;
            float moveSpeed = Mathf.MoveTowards(m_Character.Parameters.MoveSpeed, targetSpeed, speedChangeRate * Time.deltaTime);
            m_Character.Parameters.SetMove(moveInput, moveSpeed);
            ResolveMotionState();
        }

        public bool TryEvade()
        {
            m_InputBuffer.Buffer(m_EvadeState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }

        public void ResolveMotionState()
        {
            if (m_Character.Parameters.HasMoveInput)
            {
                m_Character.StateMachine.TrySetState(m_MoveState);
                return;
            }

            m_Character.StateMachine.TrySetDefaultState();
        }
    }
}
