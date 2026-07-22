using MotionCore.Infrastructure;

namespace MotionCore.Gameplay.Common
{
    /// <summary>
    /// 单个生命组件的数值变化。
    /// </summary>
    public readonly struct HealthChangedEvent : IEvent
    {
        public HealthChangedEvent(Health source)
        {
            Source = source;
            CurrentHealth = source.CurrentHealth;
            MaxHealth = source.MaxHealth;
        }

        public Health Source { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
    }
}
