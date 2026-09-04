namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 可被处决角色向攻击者暴露的最小目标能力。
    /// </summary>
    public interface IExecutionTarget
    {
        bool CanBeExecuted { get; }

        bool TryClaim();
    }
}
