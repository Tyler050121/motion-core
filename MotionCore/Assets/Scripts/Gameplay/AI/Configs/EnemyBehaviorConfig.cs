using UnityEngine;

namespace MotionCore.Gameplay.AI
{
    [CreateAssetMenu(menuName = "MotionCore/AI/Enemy Behavior Config")]
    public sealed class EnemyBehaviorConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("接近巡逻点到该距离内视为到达")]
        float m_PatrolStoppingDistance = 0.35f;
        public float PatrolStoppingDistance => m_PatrolStoppingDistance;

        [SerializeField, Tooltip("巡逻移动时是否使用跑步速度")]
        bool m_PatrolWantsRun;
        public bool PatrolWantsRun => m_PatrolWantsRun;
    }
}
