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
        LockOnTarget m_OwnerTarget;
        LockOnTarget m_CurrentTarget;

        public LockOnTarget CurrentTarget => m_CurrentTarget;
        public bool HasTarget => m_CurrentTarget != null && m_CurrentTarget.IsAvailable;

        void Start()
        {
            m_OwnerTarget = m_Character.GetComponentInChildren<LockOnTarget>(true);
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
            bool foundTarget = LockOnTargetQuery.TryFindLockTarget(
                LockOnTarget.Targets,
                m_Character.FacingRoot.position,
                m_OwnerTarget,
                m_MaxLockDistance,
                m_CenterLockRadius,
                m_Camera,
                out LockOnTarget target);
            LockOnTarget lockTarget = foundTarget ? target : null;

            return TryLockCurrentTarget(lockTarget);
        }

        public bool TryCycleLockTarget()
        {
            if (!HasTarget)
                return TryLock();

            if (!LockOnTargetQuery.TryFindNextTargetInRing(
                    LockOnTarget.Targets,
                    m_CurrentTarget,
                    m_Character.FacingRoot.position,
                    m_OwnerTarget,
                    m_MaxLockDistance,
                    m_Camera.PlanarForward,
                    out LockOnTarget nextTarget))
                return false;

            return TryLockCurrentTarget(nextTarget);
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

        bool IsTargetTooFar(LockOnTarget target)
        {
            Vector3 toTarget = target.LockPoint.position - m_Character.FacingRoot.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude > m_MaxLockDistance * m_MaxLockDistance;
        }

        bool TryLockCurrentTarget(LockOnTarget target)
        {
            if (target == null)
                return false;

            m_CurrentTarget = target;
            m_Camera.SetLockTarget(target.LockPoint);
            return true;
        }
    }
}
