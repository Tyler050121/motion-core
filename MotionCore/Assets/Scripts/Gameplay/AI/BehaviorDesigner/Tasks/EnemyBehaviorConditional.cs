#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks
{
    public abstract class EnemyBehaviorConditional : ConditionalNode
    {
        protected EnemyBehaviorController Controller { get; private set; }

        public override void OnAwake()
        {
            Controller = GetComponent<EnemyBehaviorController>();
        }
    }
}
#endif
