#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks
{
    public abstract class EnemyBehaviorAction : ActionNode
    {
        protected EnemyBehaviorController Controller { get; private set; }

        public override void OnAwake()
        {
            Controller = GetComponent<EnemyBehaviorController>();
        }

        /// <summary>
        /// 按位置来源解析出一个世界坐标，供所有需要「位置」的节点共用。
        /// </summary>
        protected Vector3 ResolvePosition(PositionSource source, SharedVariable<Vector3> variable)
        {
            return source switch
            {
                PositionSource.CurrentPosition => Controller.Position,
                PositionSource.Variable => variable.Value,
                _ => Controller.HomePosition,
            };
        }
    }
}
#endif
