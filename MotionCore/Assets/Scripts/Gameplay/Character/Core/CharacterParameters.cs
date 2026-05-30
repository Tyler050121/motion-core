using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class CharacterParameters
    {
        Vector2 m_MoveInput;
        public Vector2 MoveInput => m_MoveInput;

        Vector3 m_MoveDirection;
        public Vector3 MoveDirection => m_MoveDirection;

        float m_MoveSpeed;
        public float MoveSpeed => m_MoveSpeed;

        Vector3 m_FacingDirection;
        public Vector3 FacingDirection => m_FacingDirection;

        public bool HasFacingDirection => m_FacingDirection.sqrMagnitude > 0.0001f;

        public bool HasMoveInput => m_MoveInput.sqrMagnitude > 0.0001f;
        public bool IsRunning { get; private set; }

        public void SetMove(Vector2 moveInput, Vector3 moveDirection, float moveSpeed, bool isRunning)
        {
            m_MoveInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            m_MoveDirection = moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
            m_MoveSpeed = moveSpeed;
            IsRunning = isRunning;
        }

        public void SetFacing(Vector3 facingDirection)
        {
            facingDirection.y = 0f;
            if (facingDirection.sqrMagnitude <= 0.0001f)
            {
                ClearFacing();
                return;
            }

            m_FacingDirection = facingDirection.normalized;
        }

        public void ClearFacing()
        {
            m_FacingDirection = Vector3.zero;
        }
    }
}
