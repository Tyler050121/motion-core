using System;
using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay
{
    [CreateAssetMenu(menuName = "MotionCore/Animation/Animation Track")]
    public sealed class AnimationTrackAsset : ScriptableObject
    {
        [SerializeField, Tooltip("播放动画")]
        TransitionAsset m_Animation;
        public TransitionAsset Animation => m_Animation;

        [SerializeField] EventBinding[] m_EventBindings = Array.Empty<EventBinding>();
        public EventBinding[] EventBindings => m_EventBindings;

        [Serializable]
        public sealed class EventBinding
        {
            [SerializeField] StringAsset m_Event;
            public StringAsset Event => m_Event;

            [SerializeField] EventType m_Type;
            public EventType Type => m_Type;
        }

        public enum EventType
        {
            CanCancel,
            CanAttack,
            HitStart,
            HitEnd,
            BranchOpen,
            BranchClose,
            Feedback
        }
    }
}
