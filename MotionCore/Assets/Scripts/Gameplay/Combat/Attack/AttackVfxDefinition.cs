using System;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    public enum AttackVfxSpawnPoint
    {
        Anchor,
        Self
    }

    [Serializable]
    public sealed class AttackVfxDefinition : VfxDefinition
    {
        [SerializeField, Tooltip("生成位置")]
        AttackVfxSpawnPoint m_SpawnPoint = AttackVfxSpawnPoint.Self;
        public AttackVfxSpawnPoint SpawnPoint => m_SpawnPoint;

        [SerializeField, Tooltip("是否跟随挂点")]
        bool m_FollowSource = true;
        public bool FollowSource => m_FollowSource;

        [SerializeField, Tooltip("相对挂点的本地偏移")]
        Vector3 m_LocalOffset;
        public Vector3 LocalOffset => m_LocalOffset;

        [SerializeField, Tooltip("相对挂点的本地欧拉角")]
        Vector3 m_LocalEulerAngles;
        public Vector3 LocalEulerAngles => m_LocalEulerAngles;
    }
}
