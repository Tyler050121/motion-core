#if GRAPH_DESIGNER
using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Targeting;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Conditionals
{
    /// <summary>
    /// 检测半径内最近的敌对目标，正常执行时记录到 Controller.Target 返回 Success，否则清空返回 Failure。
    /// 作为复合节点的中断条件时只重判结果，不改写目标记忆，可在巡逻中发现目标即打断。
    /// </summary>
    [Description("检测半径内是否有最近的敌对目标，有则记录为当前目标")]
    public sealed class DetectTarget : EnemyBehaviorConditional
    {
        [Tooltip("目标阵营")]
        [SerializeField] Faction m_TargetFaction = Faction.Player;

        [Tooltip("检测半径")]
        [SerializeField] float m_DetectionRadius = 8f;

        public override TaskStatus OnUpdate()
        {
            if (!TryFindTarget(out LockOnTarget target))
            {
                Controller.ClearTarget();
                return TaskStatus.Failure;
            }

            Controller.SetTarget(target.LockPoint);
            return TaskStatus.Success;
        }

        /// <summary>
        /// Conditional Abort 重评估只负责判断是否应该切换分支，不修改 Controller 的目标记忆。
        /// 真正进入索敌分支后，Behavior Designer 会再次调用 OnUpdate，此时才提交目标。
        /// </summary>
        public override TaskStatus OnReevaluateUpdate()
        {
            return TryFindTarget(out _)
                ? TaskStatus.Success
                : TaskStatus.Failure;
        }

        bool TryFindTarget(out LockOnTarget target)
        {
            return LockOnTargetQuery.TryFindNearestTarget(
                LockOnTarget.Targets,
                Controller.Position,
                Controller.OwnerTarget,
                m_DetectionRadius,
                m_TargetFaction,
                out target);
        }
    }
}
#endif
