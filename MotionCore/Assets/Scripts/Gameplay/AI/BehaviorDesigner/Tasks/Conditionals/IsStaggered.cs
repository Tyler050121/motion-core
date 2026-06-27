#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Conditionals
{
    /// <summary>
    /// 是否处于受击硬直。受击中返回 Success，否则 Failure。
    /// 作为顶层 Selector 的最左条件并配合 LowerPriority 中断：受击即打断当前行为、整树让位，硬直结束再落回正常行为。
    /// </summary>
    [Description("是否处于受击硬直，硬直中让位整棵行为树")]
    public sealed class IsStaggered : EnemyBehaviorConditional
    {
        public override TaskStatus OnUpdate()
        {
            return Controller.IsStaggered ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
#endif
