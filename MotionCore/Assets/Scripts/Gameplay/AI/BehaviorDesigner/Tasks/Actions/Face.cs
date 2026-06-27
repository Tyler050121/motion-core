#if GRAPH_DESIGNER
using MotionCore.Gameplay.Character;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Actions
{
    /// <summary>
    /// 原地转向对准来源，与目标夹角小于阈值返回 Success，否则 Running。
    /// 通用对准节点，来源可选出生点 / 当前位置 / 共享变量 / 索敌目标，常用于攻击、技能前对准。
    /// </summary>
    [Description("原地转向对准来源（来源可选 Home/CurrentPosition/Variable/Target），对准后返回成功")]
    public sealed class Face : EnemyBehaviorAction
    {
        [Tooltip("对准来源")]
        [SerializeField] PositionSource m_Source = PositionSource.Target;

        [Tooltip("来源为 Variable 时的位置")]
        [SerializeField] SharedVariable<Vector3> m_SourceVariable;

        [Tooltip("视为对准的角度阈值（度）")]
        [SerializeField] float m_AngleThreshold = 10f;

        [Tooltip("转身快慢")]
        [SerializeField] TurnSpeed m_TurnSpeed = TurnSpeed.Combat;

        public override TaskStatus OnUpdate()
        {
            Vector3 toTarget = ResolvePosition(m_Source, m_SourceVariable) - Controller.Position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
                return TaskStatus.Success;

            Controller.FaceTo(toTarget, m_TurnSpeed);
            return Vector3.Angle(Controller.Forward, toTarget) <= m_AngleThreshold
                ? TaskStatus.Success
                : TaskStatus.Running;
        }
    }
}
#endif
