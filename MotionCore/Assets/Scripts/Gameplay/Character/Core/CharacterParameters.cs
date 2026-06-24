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

        float m_LocomotionTurnDuration = -1f;
        /// <summary>
        /// 本次移动指定的转身时长（180° 秒数）。小于 0 表示沿用 MotorConfig 默认。
        /// </summary>
        public float LocomotionTurnDuration => m_LocomotionTurnDuration;

        Vector3 m_FacingDirection;
        public Vector3 FacingDirection => m_FacingDirection;

        float m_FacingTurnDuration = -1f;
        /// <summary>
        /// 本次转身的目标时长（180° 秒数）。小于 0 表示沿用 MotorConfig 的默认转身时长。
        /// </summary>
        public float FacingTurnDuration => m_FacingTurnDuration;

        public bool HasFacingDirection => m_FacingDirection.sqrMagnitude > 0.0001f;

        public bool HasMoveInput => m_MoveInput.sqrMagnitude > 0.0001f;
        public bool IsRunning { get; private set; }

        public void SetMove(Vector2 moveInput, Vector3 moveDirection, float moveSpeed, bool isRunning, float locomotionTurnDuration = -1f)
        {
            m_MoveInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            m_MoveDirection = moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
            m_MoveSpeed = moveSpeed;
            IsRunning = isRunning;
            m_LocomotionTurnDuration = locomotionTurnDuration;
        }

        /// <summary>
        /// 设置朝向目标，并指定本次转身时长（180° 秒数）；turnDuration 小于 0 表示沿用 MotorConfig 默认。
        /// </summary>
        public void SetFacing(Vector3 facingDirection, float turnDuration = -1f)
        {
            facingDirection.y = 0f;
            if (facingDirection.sqrMagnitude <= 0.0001f)
            {
                ClearFacing();
                return;
            }

            m_FacingDirection = facingDirection.normalized;
            m_FacingTurnDuration = turnDuration;
        }

        public void ClearFacing()
        {
            m_FacingDirection = Vector3.zero;
            m_FacingTurnDuration = -1f;
        }
    }
}
