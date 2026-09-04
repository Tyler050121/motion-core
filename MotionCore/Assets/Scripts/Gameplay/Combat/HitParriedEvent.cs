using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 命中在卸势窗口内被拒绝。
    /// </summary>
    public readonly struct HitParriedEvent : IEvent
    {
        public HitParriedEvent(Transform attacker)
        {
            Attacker = attacker;
        }

        public Transform Attacker { get; }
    }
}
