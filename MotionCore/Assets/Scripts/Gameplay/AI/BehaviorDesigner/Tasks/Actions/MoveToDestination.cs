#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Actions
{
    /// <summary>
    /// 朝目标位置移动，进入停止距离内返回 Success，否则返回 Running。
    /// 通用移动节点：目标来源可选出生点 / 当前位置 / 共享变量，所以巡逻、回家、追击都复用同一个节点。
    /// 每帧重新解析来源，因此来源指向会移动的变量时（如追击目标位置）能持续跟踪。
    /// </summary>
    [Description("朝目标位置移动（来源可选 Home/CurrentPosition/Variable），进入停止距离内返回成功")]
    public sealed class MoveToDestination : EnemyBehaviorAction
    {
        [Tooltip("目标位置来源")]
        [SerializeField] PositionSource m_Source = PositionSource.Variable;

        [Tooltip("来源为 Variable 时使用的目标位置")]
        [SerializeField] SharedVariable<Vector3> m_SourceVariable;

        public override TaskStatus OnUpdate()
        {
            Vector3 offset = ResolvePosition(m_Source, m_SourceVariable) - Controller.Position;
            offset.y = 0f;

            float stoppingDistance = Controller.Config.PatrolStoppingDistance;
            if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                Controller.StopMove();
                return TaskStatus.Success;
            }

            // 边走边转：始终前进，身体平滑转向目标，走出弧线（无原地停顿）。
            Controller.MoveSteerTo(offset, Controller.Config.PatrolWantsRun);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            // 正常结束或被高优先级行为打断时都停步，避免残留移动意图。
            Controller.StopMove();
        }
    }
}
#endif
