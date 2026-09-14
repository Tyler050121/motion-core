#if GRAPH_DESIGNER
using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Targeting;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Conditionals
{
    /// <summary>
    /// 检测半径内最近的敌对目标，找到记录到 Controller.Target 返回 Success，否则清空返回 Failure。
    /// 作为复合节点的中断条件时每帧重判，可在巡逻中发现目标即打断。
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
            if (LockOnTargetQuery.TryFindNearestTarget(
                    LockOnTarget.Targets,
                    Controller.Position,
                    Controller.OwnerTarget,
                    m_DetectionRadius,
                    m_TargetFaction,
                    out LockOnTarget target))
            {
                Controller.SetTarget(target.LockPoint);
                return TaskStatus.Success;
            }

            Controller.ClearTarget();
            return TaskStatus.Failure;
        }
    }
}
#endif
