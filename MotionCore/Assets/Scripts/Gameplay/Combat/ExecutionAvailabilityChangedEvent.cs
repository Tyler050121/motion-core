using MotionCore.Infrastructure;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 玩家附近的可处决目标状态发生变化。
    /// </summary>
    public readonly struct ExecutionAvailabilityChangedEvent : IEvent
    {
        public ExecutionAvailabilityChangedEvent(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }

        public bool IsAvailable { get; }
    }
}
