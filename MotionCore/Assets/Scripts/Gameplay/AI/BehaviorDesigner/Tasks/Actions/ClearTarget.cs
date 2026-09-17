#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Actions
{
    /// <summary>
    /// 清除当前索敌目标。用于回位分支进入时结束交战状态。
    /// </summary>
    [Description("清除当前索敌目标")]
    public sealed class ClearTarget : EnemyBehaviorAction
    {
        public override TaskStatus OnUpdate()
        {
            Controller.ClearTarget();
            return TaskStatus.Success;
        }
    }
}
#endif
