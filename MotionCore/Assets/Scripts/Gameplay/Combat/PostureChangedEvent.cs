using MotionCore.Infrastructure;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 单个架势组件的数值变化。
    /// </summary>
    public readonly struct PostureChangedEvent : IEvent
    {
        /// <param name="source">发生变化的架势组件。</param>
        /// <param name="isNewlyBroken">本次变化是否让架势从有值变为零；恢复或普通扣减时为 false。</param>
        public PostureChangedEvent(Posture source, bool isNewlyBroken)
        {
            Source = source;
            CurrentPosture = source.CurrentPosture;
            MaxPosture = source.MaxPosture;
            IsBroken = source.IsBroken;
            IsNewlyBroken = isNewlyBroken;
        }

        public Posture Source { get; }
        public float CurrentPosture { get; }
        public float MaxPosture { get; }
        /// <summary>
        /// 当前架势值是否为零。破防后在架势恢复前会持续为 true。
        /// </summary>
        public bool IsBroken { get; }

        /// <summary>
        /// 本次事件是否刚刚进入破防。只有从未破防变为破防的那次扣减才为 true。
        /// </summary>
        public bool IsNewlyBroken { get; }
    }
}
