using MotionCore.Gameplay.Combat;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [CreateAssetMenu(menuName = "MotionCore/Character/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] LocomotionAnimationProfile m_LocomotionAnimation;
        public LocomotionAnimationProfile LocomotionAnimation => m_LocomotionAnimation;

        [SerializeField] AttackDefinition m_BasicAttack;
        public AttackDefinition BasicAttack => m_BasicAttack;

        [SerializeField, Tooltip("完美闪避后轻击触发的反击")]
        AttackDefinition m_DodgeCounterAttack;
        public AttackDefinition DodgeCounterAttack => m_DodgeCounterAttack;
    }
}
