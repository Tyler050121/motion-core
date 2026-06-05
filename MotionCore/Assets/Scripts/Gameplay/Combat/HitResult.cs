namespace MotionCore.Gameplay.Combat
{
    public readonly struct HitResult
    {
        public HitResult(
            Hurtbox hurtbox,
            float damage,
            float remainingHealth,
            bool isTargetDepleted,
            float knockbackPower)
        {
            Hurtbox = hurtbox;
            Damage = damage;
            RemainingHealth = remainingHealth;
            IsTargetDepleted = isTargetDepleted;
            KnockbackPower = knockbackPower;
        }

        public Hurtbox Hurtbox { get; }
        public float Damage { get; }
        public float RemainingHealth { get; }
        public bool IsTargetDepleted { get; }
        public float KnockbackPower { get; }
    }
}
