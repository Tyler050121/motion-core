#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Conditionals
{
    /// <summary>
    /// 出生点活动范围条件。由父级复合节点的 Conditional Abort 负责打断当前行为。
    /// </summary>
    [Description("敌人是否已经超出出生点活动范围")]
    public sealed class IsOutsideHomeRange : EnemyBehaviorConditional
    {
        [SerializeField, Min(0f), Tooltip("敌人距离出生点的最大活动半径，设为 0 可关闭回位限制")]
        float m_MaxHomeDistance = 8f;

        public override TaskStatus OnUpdate()
        {
            if (m_MaxHomeDistance <= 0f)
                return TaskStatus.Failure;

            Vector3 offset = Controller.Position - Controller.HomePosition;
            offset.y = 0f;
            return offset.sqrMagnitude > m_MaxHomeDistance * m_MaxHomeDistance
                ? TaskStatus.Success
                : TaskStatus.Failure;
        }
    }
}
#endif
