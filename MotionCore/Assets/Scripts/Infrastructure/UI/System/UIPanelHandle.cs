using UnityEngine;

namespace MotionCore.Infrastructure
{
    /// <summary>
    /// 运行时面板实例句柄。
    /// </summary>
    sealed class UIPanelHandle
    {
        public string PanelID;
        public GameObject Instance;
        public bool IsClosing;
        public bool PendingShow;
        public bool IsEscapable;
        public Coroutine TransitionRoutine;
    }
}
