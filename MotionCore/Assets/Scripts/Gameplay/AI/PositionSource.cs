namespace MotionCore.Gameplay.AI
{
    /// <summary>
    /// 位置来源。供行为树节点统一描述「一个世界坐标从哪来」，避免为每种位置写专用节点。
    /// </summary>
    public enum PositionSource
    {
        /// <summary>
        /// 出生点 HomePosition。
        /// </summary>
        Home,

        /// <summary>
        /// 当前所在位置。
        /// </summary>
        CurrentPosition,

        /// <summary>
        /// 自定义共享变量。
        /// </summary>
        Variable,
    }
}
