using MotionCore.Gameplay.Combat;
using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 玩家角色生成完成。
    /// </summary>
    public readonly struct PlayerSpawnedEvent : IEvent
    {
        public PlayerSpawnedEvent(Health health, Posture posture)
        {
            Health = health;
            Posture = posture;
        }

        public Health Health { get; }
        public Posture Posture { get; }
    }
}
