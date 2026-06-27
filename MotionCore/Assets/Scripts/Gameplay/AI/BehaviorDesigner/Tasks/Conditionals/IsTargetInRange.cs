#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Conditionals
{
    /// <summary>
    /// 当前目标是否在指定半径内（平面距离）。无目标返回 Failure。
    /// 用于在攻击距离内才触发攻击、否则继续追击。
    /// </summary>
    [Description("当前目标是否在指定半径内")]
    public sealed class IsTargetInRange : EnemyBehaviorConditional
    {
        [Tooltip("判定半径")]
        [SerializeField] float m_Range = 1.8f;

        public override TaskStatus OnUpdate()
        {
            if (!Controller.HasTarget)
                return TaskStatus.Failure;

            Vector3 offset = Controller.Target.position - Controller.Position;
            offset.y = 0f;
            return offset.sqrMagnitude <= m_Range * m_Range ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
#endif
