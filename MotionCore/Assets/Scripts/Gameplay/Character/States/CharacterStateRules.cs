namespace MotionCore.Gameplay.Character
{
    public static class CharacterStateRules
    {
        public static bool CanExit(CharacterStateType currentState, CharacterStateType nextState, CharacterStateExitOptions exitOptions)
        {
            if (currentState == CharacterStateType.Idle)
                return true;

            if (nextState == CharacterStateType.Hit || nextState == CharacterStateType.Dead)
                return true;

            return (exitOptions & GetRequiredOption(nextState)) != 0;
        }

        static CharacterStateExitOptions GetRequiredOption(CharacterStateType nextState)
        {
            return nextState switch
            {
                CharacterStateType.Idle => CharacterStateExitOptions.Idle,
                CharacterStateType.Move => CharacterStateExitOptions.Move,
                CharacterStateType.Evade => CharacterStateExitOptions.Evade,
                CharacterStateType.BasicAttack => CharacterStateExitOptions.Attack,
                CharacterStateType.HeavyAttack => CharacterStateExitOptions.Attack,
                CharacterStateType.Skill => CharacterStateExitOptions.Skill,
                CharacterStateType.Ultimate => CharacterStateExitOptions.Skill,
                CharacterStateType.SwitchIn => CharacterStateExitOptions.Switch,
                CharacterStateType.SwitchOut => CharacterStateExitOptions.Switch,
                CharacterStateType.TurnBack => CharacterStateExitOptions.Evade,
                CharacterStateType.QuestStart => CharacterStateExitOptions.Evade,
                CharacterStateType.Hit => CharacterStateExitOptions.None,
                CharacterStateType.Dead => CharacterStateExitOptions.None,
                _ => throw new System.ArgumentOutOfRangeException(nameof(nextState), nextState, null)
            };
        }
    }
}
