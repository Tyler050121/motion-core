using Animancer;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class HitState : CharacterState
    {
        [SerializeField, Tooltip("普通受击动画")] TransitionAsset m_HitReaction;
        [SerializeField, Tooltip("击退受击动画")] TransitionAsset m_KnockbackReaction;
        [SerializeField, Min(0f), Tooltip("击退抗性")] float m_KnockbackResistance = 1f;

        float m_KnockbackPower;
        bool m_CanInterruptSelf;

        protected override bool CanInterruptSelf => m_CanInterruptSelf;
        public override CharacterStateType Type => CharacterStateType.Hit;

        public void SetContext(float knockbackPower)
        {
            m_KnockbackPower = knockbackPower;
        }

        void OnEnable()
        {
            m_CanInterruptSelf = false;
            ExitOptions = CharacterStateExitOptions.Default; // 可以通过闪避打断

            TransitionAsset reaction = SelectReaction();
            AnimancerState state = Character.Animancer.Play(reaction);
            bool isNewEventSequence = state.Events(this, out AnimancerEvent.Sequence events);

            if (isNewEventSequence)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (events.GetName(i) == GlobalConfig.AnimationEventNames.CanCancel)
                        events.SetCallback(i, OpenCancel);
                    else if (events.GetName(i) == GlobalConfig.AnimationEventNames.CanInterrupt)
                        events.SetCallback(i, () => m_CanInterruptSelf = true);
                }
            }

            events.OnEnd = ReturnToDefaultState;
        }

        /// <summary>
        /// 根据击退力度选择受击动画。击退力度大于抗性时播放击退受击动画，否则播放普通受击动画。
        /// </summary>
        /// <returns></returns>
        TransitionAsset SelectReaction()
        {
            if (m_KnockbackPower > m_KnockbackResistance && m_KnockbackReaction != null)
                return m_KnockbackReaction;

            return m_HitReaction;
        }
    }
}
