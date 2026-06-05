using System;
using UnityEngine;

namespace MotionCore.Infrastructure
{
    [Serializable]
    public readonly struct VfxSpawnRequest
    {
        public VfxSpawnRequest(
            string assetKey,
            VfxReuseMode reuseMode,
            Vector3 position,
            Quaternion rotation,
            float scale,
            float speed,
            Transform parent,
            float releaseDelay,
            int initialCapacity,
            int minCachedCount)
        {
            AssetKey = assetKey;
            ReuseMode = reuseMode;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Speed = speed;
            Parent = parent;
            ReleaseDelay = releaseDelay;
            InitialCapacity = initialCapacity;
            MinCachedCount = minCachedCount;
        }

        public string AssetKey { get; }
        public VfxReuseMode ReuseMode { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Scale { get; }
        public float Speed { get; }
        public Transform Parent { get; }
        public float ReleaseDelay { get; }
        public int InitialCapacity { get; }
        public int MinCachedCount { get; }
    }
}
