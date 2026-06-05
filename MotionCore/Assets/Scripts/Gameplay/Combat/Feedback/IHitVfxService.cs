namespace MotionCore.Gameplay.Combat
{
    public interface IHitVfxService
    {
        void Play(in HitFeedbackContext feedback);
    }
}
