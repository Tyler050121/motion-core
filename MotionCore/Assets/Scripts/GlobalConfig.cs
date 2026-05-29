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

        public static class Timer
        {
            public const int InvalidId = -1;
        }

        public static class AnimationEventNames
        {
            public const string CanCancel = "CanCancel";
            public const string CanAttack = "CanAttack";
            public const string Hit = "Hit";
            public const string HitStart = "HitStart";
            public const string HitEnd = "HitEnd";
            public const string BranchOpen = "BranchOpen";
            public const string BranchClose = "BranchClose";
            public const string Feedback = "Feedback";
        }
    }
}
