using Animancer.Units;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [CreateAssetMenu(menuName = "MotionCore/Character/Motor Config")]
    public sealed class MotorConfig : ScriptableObject
    {
        [SerializeField] float m_WalkSpeed = 1f;
        public float WalkSpeed => m_WalkSpeed;

        [SerializeField] float m_RunSpeed = 3f;
        public float RunSpeed => m_RunSpeed;

        [SerializeField] float m_WalkSpeedChangeRate = 8f;
        public float WalkSpeedChangeRate => m_WalkSpeedChangeRate;

        [SerializeField] float m_RunSpeedChangeRate = 3f;
        public float RunSpeedChangeRate => m_RunSpeedChangeRate;

        [SerializeField] float m_RunTurnBackAngle = 135f;
        public float RunTurnBackAngle => m_RunTurnBackAngle;

        [SerializeField, Seconds] float m_RunTurnBackCooldown = 1f;
        public float RunTurnBackCooldown => m_RunTurnBackCooldown;

        [SerializeField, Seconds, Min(0f), Tooltip("180 度转身秒数")]
        float m_FacingTurnDuration = 0.2f;
        public float FacingTurnDuration => m_FacingTurnDuration;

        public float RunThresholdSpeed => (m_WalkSpeed + m_RunSpeed) * 0.5f;
    }
}
