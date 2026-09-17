using Unity.Cinemachine;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Cameras
{
    [DisallowMultipleComponent]
    public sealed class CameraController : MonoBehaviour, ICameraService
    {
        [SerializeField, Tooltip("视图相机")] Camera m_ViewCamera;
        [SerializeField, Tooltip("自由视角相机")] CinemachineCamera m_FreeLookCamera;
        [SerializeField, Tooltip("自由视角轨道控制")] CinemachineOrbitalFollow m_OrbitalFollow;
        [SerializeField, Min(0f), Tooltip("锁定转向速度")] float m_LockYawSpeed = 280f;
        [SerializeField, Min(0f), Tooltip("锁定死区角度")] float m_LockYawDeadZone = 7f;
        [SerializeField, Min(0.01f), Tooltip("锁定平滑时间")] float m_LockYawSmoothTime = 0.12f;
        [SerializeField, Min(0f), Tooltip("近距离")] float m_CloseLockDistance = 2.5f;
        [SerializeField, Min(0f), Tooltip("远距离")] float m_FarLockDistance = 7f;
        [SerializeField, Range(0f, 1f), Tooltip("近距离速度倍率")] float m_CloseLockYawSpeedScale = 0.35f;

        Transform m_LockTarget;
        float m_LockYawVelocity;

        public Vector3 PlanarForward
        {
            get
            {
                Vector3 forward = m_ViewCamera.transform.forward;
                forward.y = 0f;
                return forward.normalized;
            }
        }

        public Vector3 PlanarRight => Vector3.Cross(Vector3.up, PlanarForward).normalized;

        public Vector3 WorldToViewportPoint(Vector3 worldPosition)
        {
            return m_ViewCamera.WorldToViewportPoint(worldPosition);
        }

        void Awake()
        {
            ServiceLocator.Register<ICameraService>(this);
        }

        void LateUpdate()
        {
            if (m_LockTarget == null)
                return;

            TurnFreeLookTowardLockTarget();
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister<ICameraService>(this);
        }

        public void SetFollowTarget(Transform target)
        {
            m_FreeLookCamera.Follow = target;
            m_FreeLookCamera.LookAt = target;
        }

        public void SetLockTarget(Transform target)
        {
            m_LockTarget = target;
            m_LockYawVelocity = 0f;
        }

        public void ClearLockTarget()
        {
            m_LockTarget = null;
            m_LockYawVelocity = 0f;
        }

        void TurnFreeLookTowardLockTarget()
        {
            Vector3 direction = m_LockTarget.position - m_FreeLookCamera.Follow.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance <= 0.0001f)
                return;

            float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float yawDelta = Mathf.DeltaAngle(m_OrbitalFollow.HorizontalAxis.Value, targetYaw);
            if (Mathf.Abs(yawDelta) <= m_LockYawDeadZone)
            {
                m_LockYawVelocity = 0f;
                return;
            }

            float distanceT = Mathf.InverseLerp(m_CloseLockDistance, m_FarLockDistance, distance);
            float yawSpeedScale = Mathf.Lerp(m_CloseLockYawSpeedScale, 1f, distanceT);
            m_OrbitalFollow.HorizontalAxis.Value = Mathf.SmoothDampAngle(
                m_OrbitalFollow.HorizontalAxis.Value,
                targetYaw,
                ref m_LockYawVelocity,
                m_LockYawSmoothTime,
                m_LockYawSpeed * yawSpeedScale,
                Time.deltaTime);
        }
    }
}
