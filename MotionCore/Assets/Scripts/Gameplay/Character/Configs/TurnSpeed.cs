namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 转身速度选择，映射到 CharacterStat 的转身时长。
    /// </summary>
    public enum TurnSpeed
    {
        /// <summary>
        /// 移动转身（LocomotionTurnDuration，慢，巡逻用）。
        /// </summary>
        Locomotion,

        /// <summary>
        /// 通用朝向（FacingTurnDuration，快，非战斗转向）。
        /// </summary>
        General,

        /// <summary>
        /// 战斗瞄准（CombatTurnDuration，精确转向，攻击/对准前用）。
        /// </summary>
        Combat,
    }
}
