namespace MotionCore.Gameplay.Character
{
    public static class CharacterStateRules
    {
        public static bool CanExit(CharacterStateType currentState, CharacterStateType nextState, CharacterStateExitMode exitMode)
        {
            if (nextState == CharacterStateType.Idle)
                return false;

            if (currentState == CharacterStateType.Idle || currentState == CharacterStateType.Move)
                return true;

            if (nextState == CharacterStateType.Hit || nextState == CharacterStateType.Dead)
                return true;

            return exitMode == CharacterStateExitMode.CanCancel;
        }
    }
}
