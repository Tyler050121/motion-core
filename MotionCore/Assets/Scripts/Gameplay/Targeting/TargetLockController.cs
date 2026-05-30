using System.Collections.Generic;
using MotionCore.Gameplay.Cameras;
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

        ICameraService m_Camera;
        LockOnTarget m_CurrentTarget;

        public LockOnTarget CurrentTarget => m_CurrentTarget;
        public bool HasTarget => m_CurrentTarget != null && m_CurrentTarget.IsAvailable;

        void Start()
        {
            m_Camera = ServiceLocator.Resolve<ICameraService>();
        }

        void Update()
        {
            if (m_CurrentTarget == null)
                return;

            if (!m_CurrentTarget.IsAvailable || IsTargetTooFar(m_CurrentTarget))
                ClearLock();
        }

        public void ToggleLock()
        {
            if (m_CurrentTarget != null)
            {
                ClearLock();
                return;
            }

            TryLock();
        }

        public bool TryLock()
        {
            LockOnTarget target = FindLockTarget();
            if (target == null)
                return false;

            m_CurrentTarget = target;
            m_Camera.SetLockTarget(target.LockPoint);
            return true;
        }

        public void ClearLock()
        {
            m_CurrentTarget = null;
            m_Camera.ClearLockTarget();
        }

        public Vector3 GetDirectionFrom(Vector3 origin)
        {
            Vector3 direction = m_CurrentTarget.LockPoint.position - origin;
            direction.y = 0f;
            return direction.normalized;
        }

        LockOnTarget FindLockTarget()
        {
            Vector3 origin = m_Character.FacingRoot.position;
            float maxDistanceSqr = m_MaxLockDistance * m_MaxLockDistance;
            float centerRadiusSqr = m_CenterLockRadius * m_CenterLockRadius;

            LockOnTarget centerTarget = null;
            float centerTargetSqr = float.PositiveInfinity;
            LockOnTarget nearestTarget = null;
            float nearestTargetSqr = float.PositiveInfinity;

            IReadOnlyCollection<LockOnTarget> targets = LockOnTarget.Targets;
            foreach (LockOnTarget target in targets)
            {
                if (target == null || !target.IsAvailable)
                    continue;

                Vector3 toTarget = target.LockPoint.position - origin;
                toTarget.y = 0f;
                float distanceSqr = toTarget.sqrMagnitude;
                if (distanceSqr > maxDistanceSqr)
                    continue;

                if (!TryGetViewportDistanceSqr(target, out float viewportDistanceSqr))
                    continue;

                if (viewportDistanceSqr <= centerRadiusSqr && viewportDistanceSqr < centerTargetSqr)
                {
                    centerTarget = target;
                    centerTargetSqr = viewportDistanceSqr;
                }

                if (distanceSqr < nearestTargetSqr)
                {
                    nearestTarget = target;
                    nearestTargetSqr = distanceSqr;
                }
            }

            return centerTarget != null ? centerTarget : nearestTarget;
        }

        bool IsTargetTooFar(LockOnTarget target)
        {
            Vector3 toTarget = target.LockPoint.position - m_Character.FacingRoot.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude > m_MaxLockDistance * m_MaxLockDistance;
        }

        bool TryGetViewportDistanceSqr(LockOnTarget target, out float distanceSqr)
        {
            Vector3 viewportPoint = m_Camera.WorldToViewportPoint(target.LockPoint.position);
            if (viewportPoint.z <= 0f)
            {
                distanceSqr = 0f;
                return false;
            }

            Vector2 centerOffset = new(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
            distanceSqr = centerOffset.sqrMagnitude;
            return true;
        }
    }
}
