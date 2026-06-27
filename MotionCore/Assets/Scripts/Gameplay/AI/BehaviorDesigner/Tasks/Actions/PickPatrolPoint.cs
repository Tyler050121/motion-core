#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Actions
{
    /// <summary>
    /// 在中心点周围的环形范围（MinRadius~MaxRadius）内随机选一个点，写入共享变量供 MoveTo 使用。
    /// 单帧完成，立即返回 Success。
    /// </summary>
    [Description("在中心点周围的环形范围内随机选取一个巡逻点，并写入目标共享变量")]
    public sealed class PickPatrolPoint : EnemyBehaviorAction
    {
        [Tooltip("巡逻圈中心来源")]
        [SerializeField] PositionSource m_CenterSource = PositionSource.Home;

        [Tooltip("中心来源为 Variable 时使用的中心点")]
        [SerializeField] SharedVariable<Vector3> m_CenterVariable;

        [Tooltip("环形内圈半径，避免选到离中心太近的点")]
        [SerializeField] float m_MinRadius;

        [Tooltip("环形外圈半径")]
        [SerializeField] float m_MaxRadius = 4f;

        [Tooltip("选取到的巡逻点输出到该共享变量")]
        [SerializeField] SharedVariable<Vector3> m_Destination;

        public override TaskStatus OnUpdate()
        {
            Vector3 center = ResolvePosition(m_CenterSource, m_CenterVariable);

            float angle = Random.value * Mathf.PI * 2f;
            float radius = Random.Range(m_MinRadius, m_MaxRadius);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            m_Destination.Value = center + offset;
            return TaskStatus.Success;
        }
    }
}
#endif
