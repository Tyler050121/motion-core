using Animancer.Units;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [CreateAssetMenu(menuName = "MotionCore/Character/Motor Config")]
    public sealed class MotorConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("移动位移缩放，1=动画原速")]
        float m_MoveSpeedScale = 1f;
        public float MoveSpeedScale => m_MoveSpeedScale;

        [SerializeField, Seconds] float m_RunTurnBackCooldown = 1f;
        public float RunTurnBackCooldown => m_RunTurnBackCooldown;

        [SerializeField, Seconds, Min(0f), Tooltip("默认 180 度转身秒数（战斗/通用朝向）")]
        float m_FacingTurnDuration = 0.2f;
        public float FacingTurnDuration => m_FacingTurnDuration;

        [SerializeField, Seconds, Min(0f), Tooltip("移动时 180 度转身秒数；通常比默认更慢，让转身摊进走路循环、不显得在原地急转")]
        float m_LocomotionTurnDuration = 0.8f;
        public float LocomotionTurnDuration => m_LocomotionTurnDuration;
    }
}
