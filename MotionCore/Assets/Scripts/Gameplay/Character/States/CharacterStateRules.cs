namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 角色当前所处的顶层状态类别。
    /// </summary>
    public enum CharacterStateType
    {
        Idle = 0, // 待机
        Move = 1, // 移动
        Evade = 2, // 闪避
        BasicAttack = 3, // 普通攻击
        HeavyAttack = 4, // 重击
        Skill = 5, // 战技
        Ultimate = 6, // 终结技
        Hit = 7, // 受击
        Dead = 8, // 死亡
        Parry = 9, // 卸势
        Defense = 10, // 防御
        PostureBreak = 11, // 破韧
        Execution = 12, // 处决
        Executed = 13, // 被处决
    }

    /// <summary>
    /// 优先级未自动放行时，由动画事件显式开启的退出窗口。
    /// </summary>
    [System.Flags]
    public enum CharacterExitWindow
    {
        None = 0, // 无窗口
        Move = 1 << 0, // 可移动
        Evade = 1 << 1, // 可闪避
        Attack = 1 << 2, // 可攻击
    }

    /// <summary>
    /// 主动动作的施法优先级。仅严格更高的优先级可以自动打断当前动作。
    /// </summary>
    public enum CastPriority
    {
        None = 0, // 无施法
        BasicAttack = 10, // 普通攻击
        HeavyAttack = 20, // 重击
        Skill = 30, // 战技
        Ultimate = 40, // 终结技
        Defense = 50, // 防御
        Evade = 60, // 闪避
        Parry = 70, // 卸势
        Execution = 80, // 处决
    }

    /// <summary>
    /// 技能僵直等级与人物状态等级共用的离散序列。技能等级达到人物状态等级时产生僵直。
    /// </summary>
    public enum StaggerLevel
    {
        None = 0, // 无僵直 / 无状态保护
        LightAttack = 10, // 轻击
        ChargedAttack = 20, // 蓄力
        MartialSkill = 30, // 武学技
        Block = 40, // 格挡
        Toughness = 50, // 强韧
        PostureBreak = 60, // 破韧
        Parry = 70, // 卸势
        GuardBreak = 80, // 破防
        Execution = 90, // 处决
    }

    public static class CharacterStateRules
    {
        /// <summary>
        /// 判断当前状态能否切换到目标状态。处决、受击与死亡优先进入；其余动作先比较施法优先级，再检查显式退出窗口。
        /// </summary>
        public static bool CanExit(
            CharacterStateType currentState,
            CharacterStateType nextState,
            CastPriority currentPriority,
            CastPriority nextPriority,
            CharacterExitWindow exitWindows)
        {
            if (currentState == CharacterStateType.Dead)
                return false;

            if (nextState == CharacterStateType.Dead)
                return true;

            if (currentState == CharacterStateType.Executed)
                return false;

            if (nextState == CharacterStateType.Execution)
                return true;

            if (currentState == CharacterStateType.PostureBreak)
                return false;

            if (currentState == CharacterStateType.Idle)
                return true;

            if (nextState == CharacterStateType.Hit || nextState == CharacterStateType.PostureBreak)
                return true;

            if (currentState != CharacterStateType.Hit
                && currentState != CharacterStateType.Dead
                && nextPriority > currentPriority)
                return true;

            return nextState switch
            {
                CharacterStateType.Idle => false,
                CharacterStateType.Move => (exitWindows & CharacterExitWindow.Move) != 0,
                CharacterStateType.Evade => (exitWindows & CharacterExitWindow.Evade) != 0,
                CharacterStateType.BasicAttack or
                CharacterStateType.HeavyAttack or
                CharacterStateType.Skill or
                CharacterStateType.Ultimate => (exitWindows & CharacterExitWindow.Attack) != 0,
                CharacterStateType.Parry => false,
                CharacterStateType.Defense => false,
                CharacterStateType.PostureBreak => false,
                CharacterStateType.Execution => false,
                CharacterStateType.Executed => false,
                CharacterStateType.Hit => false,
                CharacterStateType.Dead => false,
                _ => throw new System.ArgumentOutOfRangeException(nameof(nextState), nextState, null)
            };
        }
    }
}
