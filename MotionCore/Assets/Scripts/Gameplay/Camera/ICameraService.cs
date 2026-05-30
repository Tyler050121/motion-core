using UnityEngine;

namespace MotionCore.Gameplay.Cameras
{
    public interface ICameraService
    {
        Vector3 PlanarForward { get; }
        Vector3 PlanarRight { get; }

        Vector3 WorldToViewportPoint(Vector3 worldPosition);

        void SetLockTarget(Transform target);
        void ClearLockTarget();
    }
}
