using System;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [Serializable]
    public sealed class CharacterParameters
    {
        [SerializeField] Vector2 m_MoveInput;
        [SerializeField] float m_MoveSpeed;

        public Vector2 MoveInput => m_MoveInput;
        public float MoveSpeed => m_MoveSpeed;
        public bool HasMoveInput => m_MoveInput.sqrMagnitude > 0.0001f;

        public void SetMove(Vector2 moveInput, float moveSpeed)
        {
            m_MoveInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            m_MoveSpeed = moveSpeed;
        }
    }
}
