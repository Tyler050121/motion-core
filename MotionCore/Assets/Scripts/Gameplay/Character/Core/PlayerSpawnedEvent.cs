using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 玩家角色生成完成。
    /// </summary>
    public readonly struct PlayerSpawnedEvent : IEvent
    {
        public PlayerSpawnedEvent(Health health)
        {
            Health = health;
        }

        public Health Health { get; }
    }
}
