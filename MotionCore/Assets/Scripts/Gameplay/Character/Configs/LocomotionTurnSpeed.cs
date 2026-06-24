namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 移动转身速度选择，映射到 MotorConfig 上的两个转身时长。
    /// </summary>
    public enum LocomotionTurnSpeed
    {
        /// <summary>
        /// 移动转身（LocomotionTurnDuration，慢，巡逻用）。
        /// </summary>
        Locomotion,

        /// <summary>
        /// 通用转身（FacingTurnDuration，快，追击用）。
        /// </summary>
        General,
    }
}
