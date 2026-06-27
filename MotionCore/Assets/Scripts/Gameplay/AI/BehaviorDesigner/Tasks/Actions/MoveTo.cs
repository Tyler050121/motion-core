#if GRAPH_DESIGNER
using MotionCore.Gameplay.Character;
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
    public sealed class MoveTo : EnemyBehaviorAction
    {
        [Tooltip("目标位置来源")]
        [SerializeField] PositionSource m_Source = PositionSource.Variable;

        [Tooltip("来源为 Variable 时的目标位置")]
        [SerializeField] SharedVariable<Vector3> m_SourceVariable;

        [Tooltip("到达判定距离")]
        [SerializeField] float m_StoppingDistance = 0.35f;

        [Tooltip("是否跑步")]
        [SerializeField] bool m_WantsRun;

        [Tooltip("转身快慢")]
        [SerializeField] TurnSpeed m_TurnSpeed = TurnSpeed.Locomotion;

        public override TaskStatus OnUpdate()
        {
            Vector3 offset = ResolvePosition(m_Source, m_SourceVariable) - Controller.Position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= m_StoppingDistance * m_StoppingDistance)
            {
                Controller.StopMove();
                return TaskStatus.Success;
            }

            Controller.MoveSteerTo(offset, m_WantsRun, m_TurnSpeed);
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
