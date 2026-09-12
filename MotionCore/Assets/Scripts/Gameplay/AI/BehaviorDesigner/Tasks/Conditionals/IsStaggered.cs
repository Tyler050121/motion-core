#if GRAPH_DESIGNER
using MotionCore.Gameplay.Character;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Conditionals
{
    /// <summary>
    /// 是否处于需要让位的战斗反应或终止状态。命中、破防、被处决和死亡期间返回 Success，否则 Failure。
    /// 作为顶层 Selector 的最左条件并配合 LowerPriority 中断：行为树仍持续 Tick，但行动分支让位，状态结束后再落回正常行为。
    /// </summary>
    [Description("是否处于受击、破防、被处决或死亡状态，期间让位行动分支")]
    public sealed class IsStaggered : EnemyBehaviorConditional
    {
        public override TaskStatus OnUpdate()
        {
            CharacterStateType state = Controller.CurrentStateType;
            return state == CharacterStateType.Hit
                || state == CharacterStateType.PostureBreak
                || state == CharacterStateType.Executed
                || state == CharacterStateType.Dead
                ? TaskStatus.Success
                : TaskStatus.Failure;
        }
    }
}
#endif
