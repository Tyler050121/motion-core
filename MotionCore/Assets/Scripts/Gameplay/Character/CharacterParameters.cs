using System;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [Serializable]
    public sealed class CharacterParameters
    {
        [SerializeField] Vector2 m_MoveInput;
        public Vector2 MoveInput => m_MoveInput;

        [SerializeField] Vector3 m_MoveDirection;
        public Vector3 MoveDirection => m_MoveDirection;

        [SerializeField] float m_MoveSpeed;
        public float MoveSpeed => m_MoveSpeed;

        [SerializeField] Vector3 m_FacingDirection;
        public Vector3 FacingDirection => m_FacingDirection;

        [SerializeField] float m_FacingTurnSpeed;
        public float FacingTurnSpeed => m_FacingTurnSpeed;

        public bool HasFacingDirection => m_FacingDirection.sqrMagnitude > 0.0001f;

        public bool HasMoveInput => m_MoveInput.sqrMagnitude > 0.0001f;

        public void SetMove(Vector2 moveInput, Vector3 moveDirection, float moveSpeed)
        {
            m_MoveInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            m_MoveDirection = moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
            m_MoveSpeed = moveSpeed;
        }

        public void SetFacing(Vector3 facingDirection, float turnSpeed)
        {
            facingDirection.y = 0f;
            if (facingDirection.sqrMagnitude <= 0.0001f)
            {
                ClearFacing();
                return;
            }

            m_FacingDirection = facingDirection.normalized;
            m_FacingTurnSpeed = turnSpeed;
        }

        public void ClearFacing()
        {
            m_FacingDirection = Vector3.zero;
            m_FacingTurnSpeed = 0f;
        }
    }
}
