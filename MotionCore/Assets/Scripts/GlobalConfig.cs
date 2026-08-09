namespace MotionCore
{
    /// <summary>
    /// 全局常量配置入口。
    /// </summary>
    public static class GlobalConfig
    {
        public static class Application
        {
            public const bool RunInBackground = true;
        }

        public static class Locomotion
        {
            public const float WalkSpeed = 1f;
            public const float RunSpeed = 3f;
            public const float WalkSpeedChangeRate = 8f;
            public const float RunToWalkSpeedChangeRate = 4f;
            public const float RunSpeedChangeRate = 3f;
            public const float RunTurnBackAngle = 135f;
            public const float RunThresholdSpeed = (WalkSpeed + RunSpeed) * 0.5f;

            // 方向混合参数的阻尼时长（秒）；横移变向时在前/后/侧移动画间平滑过渡，0=不平滑。
            public const float MoveInputDamp = 0.15f;
        }

        public static class Timer
        {
            public const int InvalidId = -1;
        }

        /// <summary>
        /// 物理层名，需与 ProjectSettings/TagManager.asset 保持一致。
        /// </summary>
        public static class LayerNames
        {
            public const string Character = "Character";
            public const string Hurtbox = "Hurtbox";
        }

        /// <summary>
        /// 动画事件名，需与 Assets/ScriptableObjects/Events 下的 StringAsset 同名。
        /// </summary>
        public static class AnimationEventNames
        {
            public const string CanCancel = "CanCancel";
            public const string CanInterrupt = "CanInterrupt";
            public const string CanAttack = "CanAttack";
            public const string CanEvade = "CanEvade";
            public const string Hit = "Hit";
            public const string HitStart = "HitStart";
            public const string HitEnd = "HitEnd";
            public const string InvulnerableStart = "InvulnerableStart";
            public const string InvulnerableEnd = "InvulnerableEnd";
            public const string ParryStart = "ParryStart";
            public const string ParryEnd = "ParryEnd";
            public const string CharacterCollisionOff = "CharacterCollisionOff";
            public const string CharacterCollisionOn = "CharacterCollisionOn";
            public const string BranchOpen = "BranchOpen";
            public const string BranchClose = "BranchClose";
            public const string Feedback = "Feedback";
        }
    }
}
