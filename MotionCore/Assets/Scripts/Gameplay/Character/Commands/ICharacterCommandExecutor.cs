using System;
using MotionCore.Gameplay.Combat;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public interface ICharacterCommandExecutor
    {
        void SetAttackFacingResolver(Func<Vector3> resolver);

        void SetMoveInput(Vector2 moveInput, Vector3 moveDirection, bool wantsRun, float turnDuration = -1f);

        void SetMoveSteer(Vector3 worldHeading, bool wantsRun, TurnSpeed turnSpeed = TurnSpeed.Locomotion);

        void SetMoveStrafe(Vector3 worldMoveDirection, Vector3 worldFaceDirection, bool wantsRun, TurnSpeed turnSpeed = TurnSpeed.General);

        void StopMove();

        void SetFacingDirection(Vector3 facingDirection, TurnSpeed turnSpeed = TurnSpeed.General);

        void SetDefenseHeld(bool isHeld);

        bool TryEvade();

        bool TryParry();

        bool TryDefense();

        bool TryBasicAttack();

        bool TryAttack(AttackDefinition definition);
    }
}
