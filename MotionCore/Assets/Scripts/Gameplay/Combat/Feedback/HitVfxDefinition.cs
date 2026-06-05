using System;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    public enum HitVfxSpawnPoint
    {
        HitPoint,
        VisualPoint
    }

    [Serializable]
    public sealed class HitVfxDefinition : VfxDefinition
    {
        [SerializeField, Tooltip("特效播放点")]
        HitVfxSpawnPoint m_SpawnPoint;
        public HitVfxSpawnPoint SpawnPoint => m_SpawnPoint;

        [SerializeField, Tooltip("命中点本地偏移")]
        Vector3 m_LocalOffset;
        public Vector3 LocalOffset => m_LocalOffset;

        [SerializeField, Tooltip("是否朝命中方向旋转")]
        bool m_AlignToHitDirection;
        public bool AlignToHitDirection => m_AlignToHitDirection;
    }
}
