using System.Collections.Generic;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    public readonly struct HitFeedbackContext
    {
        public HitFeedbackContext(
            Vector3 point,
            Vector3 visualPoint,
            Vector3 direction,
            IReadOnlyList<HitVfxDefinition> vfx)
        {
            Point = point;
            VisualPoint = visualPoint;
            Direction = direction;
            Vfx = vfx;
        }

        public Vector3 Point { get; }
        public Vector3 VisualPoint { get; }
        public Vector3 Direction { get; }
        public IReadOnlyList<HitVfxDefinition> Vfx { get; }
    }
}
