using MotionCore;

namespace MotionCore.Infrastructure
{
    /// <summary>
    /// 计时器句柄，由 TimerService 自动写入和清空。
    /// </summary>
    public sealed class TimerHandle
    {
        /// <summary>
        /// 当前计时器 Id。
        /// </summary>
        public int Id { get; internal set; } = GlobalConfig.Timer.InvalidId;

        /// <summary>
        /// 是否有正在运行的计时器。
        /// </summary>
        public bool IsActive => Id != GlobalConfig.Timer.InvalidId;

        internal void Clear()
        {
            Id = GlobalConfig.Timer.InvalidId;
        }
    }
}
