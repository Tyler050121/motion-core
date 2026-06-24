using System.Collections.Generic;
using MotionCore.Gameplay.Cameras;
using UnityEngine;

namespace MotionCore.Gameplay.Targeting
{
    /// <summary>
    /// 锁定目标查询工具。
    /// 统一处理可用性过滤、首次锁定选择和环绕切换顺序。
    /// </summary>
    public static class LockOnTargetQuery
    {
        /// <summary>
        /// 判断目标是否满足查询条件。
        /// </summary>
        public static bool IsUsableTarget(LockOnTarget target)
        {
            return target != null && target.IsAvailable && target.LockPoint != null;
        }

        /// <summary>
        /// 在指定范围内找最近目标。
        /// 供敌人索敌使用，只关心距离，不关心屏幕位置。
        /// </summary>
        public static bool TryFindNearestTarget(
            IReadOnlyCollection<LockOnTarget> targets,
            Vector3 origin,
            Transform ownerRoot,
            float maxDistance,
            out LockOnTarget bestTarget)
        {
            bestTarget = null;
            float maxDistanceSqr = maxDistance * maxDistance;
            float bestDistanceSqr = float.PositiveInfinity;

            foreach (LockOnTarget candidate in targets)
            {
                if (!IsSelectable(candidate, ownerRoot))
                    continue;

                Vector3 offset = candidate.LockPoint.position - origin;
                offset.y = 0f;

                float distanceSqr = offset.sqrMagnitude;
                if (distanceSqr > maxDistanceSqr || distanceSqr >= bestDistanceSqr)
                    continue;

                bestTarget = candidate;
                bestDistanceSqr = distanceSqr;
            }

            return bestTarget != null;
        }

        /// <summary>
        /// 为玩家首次锁定寻找目标。
        /// 先用屏幕中心优先，再回退到最近目标。
        /// </summary>
        public static bool TryFindLockTarget(
            IReadOnlyCollection<LockOnTarget> targets,
            Vector3 origin,
            Transform ownerRoot,
            float maxDistance,
            float centerRadius,
            ICameraService camera,
            out LockOnTarget target)
        {
            target = null;

            float maxDistanceSqr = maxDistance * maxDistance;
            float centerRadiusSqr = centerRadius * centerRadius;

            LockOnTarget centerTarget = null;
            float centerTargetSqr = float.PositiveInfinity;
            LockOnTarget nearestTarget = null;
            float nearestTargetSqr = float.PositiveInfinity;

            foreach (LockOnTarget candidate in targets)
            {
                if (!IsSelectable(candidate, ownerRoot))
                    continue;

                Vector3 offset = candidate.LockPoint.position - origin;
                offset.y = 0f;

                float distanceSqr = offset.sqrMagnitude;
                if (distanceSqr > maxDistanceSqr)
                    continue;

                if (!TryGetViewportDistanceSqr(camera, candidate, out float viewportDistanceSqr))
                    continue;

                if (viewportDistanceSqr <= centerRadiusSqr && viewportDistanceSqr < centerTargetSqr)
                {
                    centerTarget = candidate;
                    centerTargetSqr = viewportDistanceSqr;
                }

                if (distanceSqr < nearestTargetSqr)
                {
                    nearestTarget = candidate;
                    nearestTargetSqr = distanceSqr;
                }
            }

            target = centerTarget != null ? centerTarget : nearestTarget;
            return target != null;
        }

        /// <summary>
        /// 在当前锁定基础上，按环绕顺序切换到下一个目标。
        /// </summary>
        public static bool TryFindNextTargetInRing(
            IReadOnlyCollection<LockOnTarget> targets,
            LockOnTarget currentTarget,
            Vector3 origin,
            Transform ownerRoot,
            float maxDistance,
            Vector3 referenceForward,
            out LockOnTarget nextTarget)
        {
            nextTarget = null;
            if (!IsSelectable(currentTarget, ownerRoot))
                return false;

            float maxDistanceSqr = maxDistance * maxDistance;
            List<LockOnTarget> selectableTargets = new();

            foreach (LockOnTarget candidate in targets)
            {
                if (!IsSelectable(candidate, ownerRoot))
                    continue;

                Vector3 offset = candidate.LockPoint.position - origin;
                offset.y = 0f;

                if (offset.sqrMagnitude > maxDistanceSqr)
                    continue;

                selectableTargets.Add(candidate);
            }

            if (selectableTargets.Count <= 1)
                return false;

            selectableTargets.Sort((left, right) => CompareRingOrder(left, right, origin, referenceForward));

            int currentIndex = selectableTargets.IndexOf(currentTarget);
            if (currentIndex < 0)
                return false;

            int nextIndex = (currentIndex + 1) % selectableTargets.Count;
            if (nextIndex == currentIndex)
                return false;

            nextTarget = selectableTargets[nextIndex];
            return true;
        }

        /// <summary>
        /// 过滤掉空目标、无效目标和同根节点目标。
        /// </summary>
        static bool IsSelectable(LockOnTarget target, Transform ownerRoot)
        {
            return IsUsableTarget(target) && target.transform.root != ownerRoot;
        }

        /// <summary>
        /// 先按环绕角度排序，再按距离做二级排序。
        /// </summary>
        static int CompareRingOrder(LockOnTarget left, LockOnTarget right, Vector3 origin, Vector3 referenceForward)
        {
            float leftAngle = GetRingAngle(left, origin, referenceForward);
            float rightAngle = GetRingAngle(right, origin, referenceForward);

            int angleCompare = leftAngle.CompareTo(rightAngle);
            if (angleCompare != 0)
                return angleCompare;

            float leftDistance = GetHorizontalDistanceSqr(left, origin);
            float rightDistance = GetHorizontalDistanceSqr(right, origin);
            return leftDistance.CompareTo(rightDistance);
        }

        /// <summary>
        /// 计算目标相对参考前方的水平夹角。
        /// </summary>
        static float GetRingAngle(LockOnTarget target, Vector3 origin, Vector3 referenceForward)
        {
            Vector3 offset = target.LockPoint.position - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude <= 0f)
                return 0f;

            return Vector3.SignedAngle(referenceForward, offset, Vector3.up);
        }

        /// <summary>
        /// 计算目标的水平距离平方。
        /// </summary>
        static float GetHorizontalDistanceSqr(LockOnTarget target, Vector3 origin)
        {
            Vector3 offset = target.LockPoint.position - origin;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        /// <summary>
        /// 计算目标到屏幕中心的视口距离平方。
        /// </summary>
        static bool TryGetViewportDistanceSqr(ICameraService camera, LockOnTarget target, out float distanceSqr)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(target.LockPoint.position);
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
