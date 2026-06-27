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

        [SerializeField, Seconds, Min(0f), Tooltip("通用朝向 180 度转身秒数（非战斗的快速转向）")]
        float m_FacingTurnDuration = 0.2f;
        public float FacingTurnDuration => m_FacingTurnDuration;

        [SerializeField, Seconds, Min(0f), Tooltip("移动时 180 度转身秒数；通常比默认更慢，让转身摊进走路循环、不显得在原地急转")]
        float m_LocomotionTurnDuration = 0.8f;
        public float LocomotionTurnDuration => m_LocomotionTurnDuration;

        [SerializeField, Seconds, Min(0f), Tooltip("战斗瞄准 180 度转身秒数；攻击/对准前精确转向，通常比通用更快")]
        float m_CombatTurnDuration = 0.15f;
        public float CombatTurnDuration => m_CombatTurnDuration;
    }
}
