#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Conditionals
{
    /// <summary>
    /// 攻击是否已过冷却。作攻击分支的前置条件 + engage Selector 的 LowerPriority 中断：
    /// 冷却结束即翻 Success，打断正在跑的走位（Orbit）重新进攻。
    /// </summary>
    [Description("攻击是否已过冷却")]
    public sealed class AttackReady : EnemyBehaviorConditional
    {
        public override TaskStatus OnUpdate()
        {
            return Controller.IsAttackReady ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
#endif
