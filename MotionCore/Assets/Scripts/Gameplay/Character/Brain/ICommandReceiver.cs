using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public interface ICommandReceiver
    {
        void SetMoveInput(Vector2 moveInput, bool wantsRun);

        bool TryEvade();

        bool TryAttack(AttackDefinition definition);
    }
}
