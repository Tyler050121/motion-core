using MotionCore.Gameplay.Combat;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [CreateAssetMenu(menuName = "MotionCore/Character/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] MotorConfig m_Motor;
        public MotorConfig Motor => m_Motor;

        [SerializeField] AttackDefinition m_BasicAttack;
        public AttackDefinition BasicAttack => m_BasicAttack;
    }
}
