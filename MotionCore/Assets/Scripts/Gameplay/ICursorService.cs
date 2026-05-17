namespace MotionCore.Gameplay
{
    public interface ICursorService
    {
        bool IsLocked { get; }

        void ApplyStartupState();
        void Tick();
        void HandleApplicationFocus(bool focus);
        void Lock();
        void Unlock();
        void SetLocked(bool locked);
        void SetVisible(bool visible);
    }
}
