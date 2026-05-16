using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public interface ICharacterActionPlayer
    {
        void SetMoveInput(Vector2 moveInput, bool wantsRun);

        bool TryEvade();
    }
}
