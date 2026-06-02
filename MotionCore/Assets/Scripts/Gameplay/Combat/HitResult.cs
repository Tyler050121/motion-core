using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    public readonly struct HitResult
    {
        public HitResult(
            Hurtbox hurtbox,
            float damage,
            float remainingHealth,
            bool isTargetDepleted,
            Vector3 point,
            Vector3 direction,
            float staggerPower,
            float knockbackPower)
        {
            Hurtbox = hurtbox;
            Damage = damage;
            RemainingHealth = remainingHealth;
            IsTargetDepleted = isTargetDepleted;
            Point = point;
            Direction = direction;
            StaggerPower = staggerPower;
            KnockbackPower = knockbackPower;
        }

        public Hurtbox Hurtbox { get; }
        public float Damage { get; }
        public float RemainingHealth { get; }
        public bool IsTargetDepleted { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public float StaggerPower { get; }
        public float KnockbackPower { get; }
    }
}
