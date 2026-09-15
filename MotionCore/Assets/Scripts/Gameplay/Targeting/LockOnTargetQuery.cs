using System.Collections.Generic;
using MotionCore.Gameplay.Cameras;
using MotionCore.Gameplay.Common;
using UnityEngine;

namespace MotionCore.Gameplay.Targeting
{
    /// <summary>
    /// 锁定目标查询工具。
    /// 统一处理可用性过滤、首次锁定选择和单轮切换顺序。
    /// </summary>
    public static class LockOnTargetQuery
    {
        /// <summary>
        /// 判断目标是否满足查询条件。
        /// </summary>
        public static bool IsUsableTarget(LockOnTarget target)
        {
            return target != null && target.IsAvailable;
        }

        /// <summary>
        /// 在指定范围内找最近的指定阵营目标。
        /// 供敌人索敌使用，只关心距离与阵营，不关心屏幕位置。
        /// </summary>
        public static bool TryFindNearestTarget(
            IReadOnlyCollection<LockOnTarget> targets,
            Vector3 origin,
            LockOnTarget ownerTarget,
            float maxDistance,
            Faction targetFaction,
            out LockOnTarget bestTarget)
        {
            bestTarget = null;
            float maxDistanceSqr = maxDistance * maxDistance;
            float bestDistanceSqr = float.PositiveInfinity;

            foreach (LockOnTarget candidate in targets)
            {
                if (!IsSelectable(candidate, ownerTarget, targetFaction))
                    continue;

                Vector3 offset = candidate.LockPoint.position - origin;
                offset.y = 0f;

                float distanceSqr = offset.sqrMagnitude;
                if (distanceSqr > maxDistanceSqr ||
                    !IsPreferredTarget(candidate, distanceSqr, bestTarget, bestDistanceSqr))
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
            LockOnTarget ownerTarget,
            float maxDistance,
            float centerRadius,
            Faction targetFaction,
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
                if (!IsSelectable(candidate, ownerTarget, targetFaction))
                    continue;

                Vector3 offset = candidate.LockPoint.position - origin;
                offset.y = 0f;

                float distanceSqr = offset.sqrMagnitude;
                if (distanceSqr > maxDistanceSqr)
                    continue;

                if (TryGetViewportDistanceSqr(camera, candidate, out float viewportDistanceSqr) &&
                    viewportDistanceSqr <= centerRadiusSqr &&
                    IsPreferredTarget(candidate, viewportDistanceSqr, centerTarget, centerTargetSqr))
                {
                    centerTarget = candidate;
                    centerTargetSqr = viewportDistanceSqr;
                }

                if (IsPreferredTarget(candidate, distanceSqr, nearestTarget, nearestTargetSqr))
                {
                    nearestTarget = candidate;
                    nearestTargetSqr = distanceSqr;
                }
            }

            target = centerTarget != null ? centerTarget : nearestTarget;
            return target != null;
        }

        /// <summary>
        /// 构建以当前目标为起点的单轮锁定切换序列。
        /// </summary>
        public static void FillTargetCycle(
            IReadOnlyCollection<LockOnTarget> targets,
            LockOnTarget currentTarget,
            Vector3 origin,
            LockOnTarget ownerTarget,
            float maxDistance,
            Faction targetFaction,
            List<LockOnTarget> results)
        {
            results.Clear();
            float maxDistanceSqr = maxDistance * maxDistance;

            foreach (LockOnTarget candidate in targets)
            {
                if (!IsSelectable(candidate, ownerTarget, targetFaction))
                    continue;

                Vector3 offset = candidate.LockPoint.position - origin;
                offset.y = 0f;

                if (offset.sqrMagnitude > maxDistanceSqr)
                    continue;

                results.Add(candidate);
            }

            Vector3 cycleStart = currentTarget.LockPoint.position - origin;
            cycleStart.y = 0f;
            results.Sort((left, right) => CompareCycleOrder(left, right, currentTarget, origin, cycleStart));
        }

        /// <summary>
        /// 过滤掉空目标、无效目标、自身目标和非目标阵营。
        /// </summary>
        static bool IsSelectable(LockOnTarget target, LockOnTarget ownerTarget, Faction targetFaction)
        {
            return IsUsableTarget(target) && target != ownerTarget && target.Faction == targetFaction;
        }

        /// <summary>
        /// 当前目标始终在首位；其余目标先按环绕角度，再按距离排序。
        /// </summary>
        static int CompareCycleOrder(
            LockOnTarget left,
            LockOnTarget right,
            LockOnTarget currentTarget,
            Vector3 origin,
            Vector3 cycleStart)
        {
            if (left == right)
                return 0;

            if (left == currentTarget)
                return -1;

            if (right == currentTarget)
                return 1;

            float leftAngle = GetCycleAngle(left, origin, cycleStart);
            float rightAngle = GetCycleAngle(right, origin, cycleStart);

            int angleCompare = leftAngle.CompareTo(rightAngle);
            if (angleCompare != 0)
                return angleCompare;

            float leftDistance = GetHorizontalDistanceSqr(left, origin);
            float rightDistance = GetHorizontalDistanceSqr(right, origin);
            int distanceCompare = leftDistance.CompareTo(rightDistance);
            if (distanceCompare != 0)
                return distanceCompare;

            return left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        /// <summary>
        /// 比较同一查询中的目标，避免集合遍历顺序影响最终选择。
        /// </summary>
        static bool IsPreferredTarget(
            LockOnTarget candidate,
            float candidateScore,
            LockOnTarget current,
            float currentScore)
        {
            if (current == null)
                return true;

            int scoreComparison = candidateScore.CompareTo(currentScore);
            return scoreComparison < 0 ||
                   scoreComparison == 0 && candidate.GetInstanceID() < current.GetInstanceID();
        }

        /// <summary>
        /// 计算目标相对参考前方的水平夹角。
        /// </summary>
        static float GetCycleAngle(LockOnTarget target, Vector3 origin, Vector3 cycleStart)
        {
            Vector3 offset = target.LockPoint.position - origin;
            offset.y = 0f;
            float angle = Vector3.SignedAngle(cycleStart, offset, Vector3.up);
            return angle < 0f ? angle + 360f : angle;
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
