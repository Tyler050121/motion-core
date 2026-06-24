namespace MotionCore.Gameplay.Common
{
    /// <summary>
    /// 阵营。用于索敌等场景区分敌我。
    /// 默认值取 Enemy（敌人通常远多于玩家），玩家需在其上显式标记为 Player。
    /// </summary>
    public enum Faction
    {
        /// <summary>
        /// 敌人阵营（默认）。
        /// </summary>
        Enemy,

        /// <summary>
        /// 玩家阵营。
        /// </summary>
        Player,

        /// <summary>
        /// 中立，不与任何阵营敌对。
        /// </summary>
        Neutral,
    }
}
