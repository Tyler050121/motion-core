using System;
using UnityEngine;

namespace MotionCore.Infrastructure
{
    [Serializable]
    public abstract class VfxDefinition
    {
        [SerializeField, Tooltip("Resources 下的特效 prefab 路径")]
        string m_AssetKey;
        public string AssetKey => m_AssetKey;

        [SerializeField, Tooltip("实例生命周期")]
        VfxReuseMode m_ReuseMode = VfxReuseMode.Pooled;
        public VfxReuseMode ReuseMode => m_ReuseMode;

        [SerializeField, Min(0.1f), Tooltip("特效缩放倍数")]
        float m_Scale = 1f;
        public float Scale => Mathf.Max(0.1f, m_Scale);

        [SerializeField, Min(0.1f), Tooltip("播放速度")]
        float m_Speed = 1f;
        public float Speed => m_Speed;

        [SerializeField, Min(0f), Tooltip("播放结束后归还秒数")]
        float m_ReleaseDelay;
        public float ReleaseDelay => m_ReleaseDelay;

        [SerializeField, Min(0), Tooltip("初始预热数量")]
        [ShowIf(nameof(m_ReuseMode), VfxReuseMode.Pooled)]
        int m_InitialCapacity;
        public int InitialCapacity => m_InitialCapacity;

        [SerializeField, Min(0), Tooltip("最小缓存数量")]
        [ShowIf(nameof(m_ReuseMode), VfxReuseMode.Pooled)]
        int m_MinCachedCount;
        public int MinCachedCount => m_MinCachedCount;
    }
}
