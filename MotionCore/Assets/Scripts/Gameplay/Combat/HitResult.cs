using MotionCore.Gameplay.Character;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    public readonly struct HitResult
    {
        public HitResult(
            Hurtbox hurtbox,
            Transform attacker,
            float damage,
            float remainingHealth,
            bool isTargetDepleted,
            StaggerLevel staggerLevel,
            float knockbackPower)
        {
            Hurtbox = hurtbox;
            Attacker = attacker;
            Damage = damage;
            RemainingHealth = remainingHealth;
            IsTargetDepleted = isTargetDepleted;
            StaggerLevel = staggerLevel;
            KnockbackPower = knockbackPower;
        }

        public Hurtbox Hurtbox { get; }

        /// <summary>
        /// 发起本次攻击的来源（攻击者武器/骨骼节点），可用 root 取其角色根；供受击方仇恨反击。
        /// </summary>
        public Transform Attacker { get; }

        public float Damage { get; }
        public float RemainingHealth { get; }
        public bool IsTargetDepleted { get; }
        public StaggerLevel StaggerLevel { get; }
        public float KnockbackPower { get; }
    }
}
