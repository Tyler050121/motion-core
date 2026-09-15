using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Targeting
{
    /// <summary>
    /// 锁定目标发生变化。
    /// </summary>
    public readonly struct TargetLockChangedEvent : IEvent
    {
        public TargetLockChangedEvent(GameObject source, LockOnTarget target)
        {
            Source = source;
            Target = target;
        }

        public GameObject Source { get; }
        public LockOnTarget Target { get; }
    }
}
