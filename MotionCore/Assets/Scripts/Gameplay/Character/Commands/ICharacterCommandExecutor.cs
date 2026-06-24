using System;
using MotionCore.Gameplay.Combat;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public interface ICharacterCommandExecutor
    {
        void SetAttackFacingResolver(Func<Vector3> resolver);

        void SetMoveInput(Vector2 moveInput, Vector3 moveDirection, bool wantsRun, float turnDuration = -1f);

        void SetMoveSteer(Vector3 worldHeading, bool wantsRun, LocomotionTurnSpeed turnSpeed = LocomotionTurnSpeed.Locomotion);

        void StopMove();

        void SetFacingDirection(Vector3 facingDirection);

        bool TryEvade();

        bool TryBasicAttack();

        bool TryAttack(AttackDefinition definition);
    }
}
