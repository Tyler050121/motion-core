using MotionCore.Infrastructure;

namespace MotionCore.Gameplay.Combat
{
    public readonly struct HitEvent : IEvent
    {
        public HitEvent(HitResult result, HitFeedbackContext feedback)
        {
            Result = result;
            Feedback = feedback;
        }

        public HitResult Result { get; }
        public HitFeedbackContext Feedback { get; }
    }
}
