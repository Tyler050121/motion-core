using System.Collections.Generic;
using MotionCore.Gameplay.Cameras;
using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;
using UnityEngine;
using CharacterContext = MotionCore.Gameplay.Character.Character;

namespace MotionCore.Gameplay.Targeting
{
    /// <summary>
    /// 负责锁定选择与锁定生命周期。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TargetLockController : MonoBehaviour
    {
        [SerializeField, Tooltip("角色")] CharacterContext m_Character;
        [SerializeField, Min(0f), Tooltip("最大锁定距离")] float m_MaxLockDistance = 12f;
        [SerializeField, Range(0f, 1f), Tooltip("屏幕中心半径")] float m_CenterLockRadius = 0.25f;
        [SerializeField, Tooltip("可锁定的目标阵营")] Faction m_TargetFaction = Faction.Enemy;

        ICameraService m_Camera;
        IEventBus m_EventBus;
        LockOnTarget m_OwnerTarget;
        LockOnTarget m_CurrentTarget;
        readonly List<LockOnTarget> m_TargetCycle = new();
        int m_TargetCycleIndex;

        public LockOnTarget CurrentTarget => m_CurrentTarget;
        public bool HasTarget => LockOnTargetQuery.IsUsableTarget(m_CurrentTarget);

        void Start()
        {
            m_OwnerTarget = m_Character.GetComponentInChildren<LockOnTarget>(true);
            m_Camera = ServiceLocator.Resolve<ICameraService>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
            m_EventBus.Publish(new TargetLockChangedEvent(gameObject, m_CurrentTarget));
        }

        void OnDisable()
        {
            ClearLock();
        }

        void Update()
        {
            if (ReferenceEquals(m_CurrentTarget, null))
                return;

            if (!LockOnTargetQuery.IsUsableTarget(m_CurrentTarget) || IsTargetTooFar(m_CurrentTarget))
                ClearLock();
        }

        public void ToggleLock()
        {
            if (HasTarget)
            {
                ClearLock();
                return;
            }

            TryLock();
        }

        public bool TryLock()
        {
            if (!LockOnTargetQuery.TryFindLockTarget(
                    LockOnTarget.Targets,
                    m_Character.FacingRoot.position,
                    m_OwnerTarget,
                    m_MaxLockDistance,
                    m_CenterLockRadius,
                    m_TargetFaction,
                    m_Camera,
                    out LockOnTarget target))
            {
                ClearLock();
                return false;
            }

            BeginTargetCycle(target);
            return true;
        }

        public bool TryCycleLockTarget()
        {
            if (!HasTarget)
                return TryLock();

            while (++m_TargetCycleIndex < m_TargetCycle.Count)
            {
                LockOnTarget target = m_TargetCycle[m_TargetCycleIndex];
                if (!LockOnTargetQuery.IsUsableTarget(target) ||
                    IsTargetTooFar(target))
                    continue;

                SetTarget(target);
                return true;
            }

            ClearLock();
            return true;
        }

        public void ClearLock()
        {
            if (ReferenceEquals(m_CurrentTarget, null))
                return;

            m_CurrentTarget = null;
            m_TargetCycle.Clear();
            m_TargetCycleIndex = 0;
            m_Camera.ClearLockTarget();
            m_EventBus.Publish(new TargetLockChangedEvent(gameObject, null));
        }

        public Vector3 GetDirectionFrom(Vector3 origin)
        {
            Vector3 direction = m_CurrentTarget.LockPoint.position - origin;
            direction.y = 0f;
            return direction.normalized;
        }

        bool IsTargetTooFar(LockOnTarget target)
        {
            Vector3 toTarget = target.LockPoint.position - m_Character.FacingRoot.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude > m_MaxLockDistance * m_MaxLockDistance;
        }

        void SetTarget(LockOnTarget target)
        {
            m_CurrentTarget = target;
            m_Camera.SetLockTarget(target.LockPoint);
            m_EventBus.Publish(new TargetLockChangedEvent(gameObject, target));
        }

        void BeginTargetCycle(LockOnTarget target)
        {
            LockOnTargetQuery.FillTargetCycle(
                LockOnTarget.Targets,
                target,
                m_Character.FacingRoot.position,
                m_OwnerTarget,
                m_MaxLockDistance,
                m_TargetFaction,
                m_TargetCycle);
            m_TargetCycleIndex = 0;
            SetTarget(target);
        }
    }
}
