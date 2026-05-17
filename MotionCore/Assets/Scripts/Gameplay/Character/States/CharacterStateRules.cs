namespace MotionCore.Gameplay.Character
{
    public static class CharacterStateRules
    {
        public static bool CanExit(CharacterStateType currentState, CharacterStateType nextState, CharacterStateExitPhase exitPhase)
        {
            if (currentState == CharacterStateType.Idle)
                return true;

            if (nextState == CharacterStateType.Idle)
                return exitPhase == CharacterStateExitPhase.Finished;

            if (nextState == CharacterStateType.Hit || nextState == CharacterStateType.Dead)
                return true;

            return exitPhase == CharacterStateExitPhase.CanCancel;
        }
    }
}
