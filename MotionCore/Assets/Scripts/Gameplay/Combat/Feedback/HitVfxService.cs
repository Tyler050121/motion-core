using System;
using System.Collections.Generic;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DefaultExecutionOrder(-9990)]
    [DisallowMultipleComponent]
    public sealed class HitVfxService : MonoBehaviour, IHitVfxService
    {
        IVfxService m_Vfx;

        void Awake()
        {
            m_Vfx = ServiceLocator.Resolve<IVfxService>();
            ServiceLocator.Register<IHitVfxService>(this);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister<IHitVfxService>(this);
        }

        public void Play(in HitFeedbackContext feedback)
        {
            IReadOnlyList<HitVfxDefinition> definitions = feedback.Vfx;
            for (int i = 0; i < definitions.Count; i++)
            {
                Play(feedback, definitions[i]);
            }
        }

        void Play(in HitFeedbackContext feedback, in HitVfxDefinition definition)
        {
            m_Vfx.Play(definition.Preset, new VfxSpawnRequest(
                ResolvePosition(feedback, definition) + definition.LocalOffset,
                ResolveRotation(feedback, definition),
                definition.Scale,
                definition.Speed,
                null));
        }

        static Vector3 ResolvePosition(in HitFeedbackContext feedback, in HitVfxDefinition definition)
        {
            if (definition.SpawnPoint == HitVfxSpawnPoint.VisualPoint)
                return feedback.VisualPoint;

            return feedback.Point;
        }

        static Quaternion ResolveRotation(in HitFeedbackContext feedback, in HitVfxDefinition definition)
        {
            if (!definition.AlignToHitDirection)
                return Quaternion.identity;

            Vector3 direction = feedback.Direction;
            if (direction.sqrMagnitude <= 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
