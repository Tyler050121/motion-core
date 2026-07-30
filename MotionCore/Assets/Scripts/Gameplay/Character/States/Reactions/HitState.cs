using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class HitState : CharacterState
    {
        [SerializeField, Tooltip("普通受击动画")] TransitionAsset m_HitReaction;
        [SerializeField, Tooltip("击退受击动画")] TransitionAsset m_KnockbackReaction;
        [SerializeField, Min(0f), Tooltip("击退抗性")] float m_KnockbackResistance = 1f;

        float m_KnockbackPower;

        public override CharacterStateType Type => CharacterStateType.Hit;

        public void SetContext(float knockbackPower)
        {
            m_KnockbackPower = knockbackPower;
        }

        void OnEnable()
        {
            PlayWithEvents(SelectReaction(), ReturnToDefaultState);
        }

        /// <summary>
        /// 根据击退力度选择受击动画。击退力度大于抗性时播放击退受击动画，否则播放普通受击动画。
        /// </summary>
        TransitionAsset SelectReaction()
        {
            if (m_KnockbackPower > m_KnockbackResistance && m_KnockbackReaction != null)
                return m_KnockbackReaction;

            return m_HitReaction;
        }
    }
}
