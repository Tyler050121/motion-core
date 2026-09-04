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
        [SerializeField, Tooltip("所属阵营")] Faction m_Faction;

        Health m_Health;

        public static IReadOnlyCollection<LockOnTarget> Targets => s_Targets;
        public Transform LockPoint => m_LockPoint;
        public Health Health => m_Health;
        public Faction Faction => m_Faction;
        public bool IsAvailable => isActiveAndEnabled && !m_Health.IsDead;

        void Awake()
        {
            m_Health = GetComponentInParent<Health>();
        }

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
