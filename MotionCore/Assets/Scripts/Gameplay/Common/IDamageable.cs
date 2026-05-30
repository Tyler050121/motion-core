namespace MotionCore.Gameplay.Common
{
    /// <summary>
    /// 可被伤害影响的目标接口。
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        bool IsDepleted { get; }

        void ApplyDamage(float damage);
    }
}
