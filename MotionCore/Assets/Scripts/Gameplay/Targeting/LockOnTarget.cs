using System.Collections.Generic;
using MotionCore.Gameplay.Common;
using UnityEngine;

namespace MotionCore.Gameplay.Targeting
{
    [DisallowMultipleComponent]
    public sealed class LockOnTarget : MonoBehaviour
    {
        static readonly HashSet<LockOnTarget> s_Targets = new();

        [SerializeField, Tooltip("锁定点")] Transform m_LockPoint;
        [SerializeField, Tooltip("生命组件")] Health m_Health;

        public static IReadOnlyCollection<LockOnTarget> Targets => s_Targets;
        public Transform LockPoint => m_LockPoint;
        public bool IsAvailable => isActiveAndEnabled && !m_Health.IsDepleted;

        void OnEnable()
        {
            s_Targets.Add(this);
        }

        void OnDisable()
        {
            s_Targets.Remove(this);
        }
    }
}
