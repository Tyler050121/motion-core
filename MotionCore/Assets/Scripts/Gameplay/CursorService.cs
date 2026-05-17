using UnityEngine;

namespace MotionCore.Gameplay
{
    public sealed class CursorService : ICursorService
    {
        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        public void ApplyStartupState()
        {
            Lock();
        }

        public void Tick()
        {
            bool showCursor = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            if (showCursor && IsLocked)
                Unlock();

            if (!showCursor && !IsLocked)
                Lock();
        }

        public void HandleApplicationFocus(bool focus)
        {
            if (!focus)
                Unlock();
        }

        public void Lock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetLocked(bool locked)
        {
            if (locked)
                Lock();
            else
                Unlock();
        }

        public void SetVisible(bool visible)
        {
            Cursor.visible = visible;
        }
    }
}
