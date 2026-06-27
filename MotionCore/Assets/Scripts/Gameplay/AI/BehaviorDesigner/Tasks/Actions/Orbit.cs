#if GRAPH_DESIGNER
using MotionCore.Gameplay.Character;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Actions
{
    /// <summary>
    /// 环绕方向（俯视角下的旋转方向）。
    /// </summary>
    public enum OrbitDirection
    {
        /// <summary>
        /// 顺时针。
        /// </summary>
        Clockwise,

        /// <summary>
        /// 逆时针。
        /// </summary>
        CounterClockwise,
    }

    /// <summary>
    /// 绕中心横移环绕：朝向锁定中心，沿切线移动，同时按半径误差做径向收敛。
    /// 通用环绕节点，中心来源可选出生点 / 当前位置 / 共享变量 / 索敌目标，绕人、绕点、绕守卫位复用同一节点。
    /// 每帧重新解析中心，来源为移动目标时持续跟随环绕。
    /// 中心来源为 Target 且无目标时返回 Failure，否则持续返回 Running，由父级中断或计时结束。
    /// </summary>
    [Description("绕中心横移环绕（来源可选 Home/CurrentPosition/Variable/Target），朝向锁定中心并维持半径")]
    public sealed class Orbit : EnemyBehaviorAction
    {
        [Tooltip("环绕中心来源")]
        [SerializeField] PositionSource m_CenterSource = PositionSource.Target;

        [Tooltip("来源为 Variable 时的中心位置")]
        [SerializeField] SharedVariable<Vector3> m_CenterVariable;

        [Tooltip("环绕半径")]
        [SerializeField] float m_Radius = 3f;

        [Tooltip("径向收敛范围：半径误差在此范围内按比例修正，越小越贴合半径")]
        [SerializeField] float m_RadiusGain = 1f;

        [Tooltip("环绕方向")]
        [SerializeField] OrbitDirection m_Direction = OrbitDirection.Clockwise;

        [Tooltip("是否跑步")]
        [SerializeField] bool m_WantsRun;

        public override TaskStatus OnUpdate()
        {
            Vector3 toCenter = ResolvePosition(m_CenterSource, m_CenterVariable) - Controller.Position;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude <= 0.0001f)
                return TaskStatus.Running;

            float distance = toCenter.magnitude;
            Vector3 toCenterDir = toCenter / distance;

            // 切向分量决定环绕方向。
            float sign = m_Direction == OrbitDirection.Clockwise ? 1f : -1f;
            Vector3 tangent = Vector3.Cross(Vector3.up, toCenterDir) * sign;

            // 径向分量按半径误差比例收敛并限幅：误差越界饱和为 ±1，在半径上时归零，全程连续无跳变。
            float radial = Mathf.Clamp((distance - m_Radius) / m_RadiusGain, -1f, 1f);
            Vector3 move = tangent + toCenterDir * radial;

            // 朝向始终对准中心，移动方向喂方向混合播出侧 / 后移动画。
            Controller.MoveStrafe(move.normalized, toCenterDir, m_WantsRun);
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
