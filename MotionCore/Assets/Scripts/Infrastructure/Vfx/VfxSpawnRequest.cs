using System;
using UnityEngine;

namespace MotionCore.Infrastructure
{
    [Serializable]
    public readonly struct VfxSpawnRequest
    {
        public VfxSpawnRequest(
            Vector3 position,
            Quaternion rotation,
            float scale,
            float speed,
            Transform parent)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Speed = speed;
            Parent = parent;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Scale { get; }
        public float Speed { get; }
        public Transform Parent { get; }
    }
}
