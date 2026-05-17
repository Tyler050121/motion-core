using MotionCore.Bootstrap;
using MotionCore.Gameplay;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.ApplicationLifecycle
{
    /// <summary>
    /// 游戏应用生命周期入口。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class ApplicationController : MonoBehaviour
    {
        ICursorService m_Cursor;
        ITimerService m_Timer;
        bool m_RuntimeStarted;

        void Awake()
        {
            Application.runInBackground = GlobalConfig.Application.RunInBackground;
            m_Cursor = new CursorService();
            m_Timer = new TimerService();
            ServiceLocator.Register(m_Cursor);
            ServiceLocator.Register(m_Timer);
        }

        void Start()
        {
            m_Cursor.ApplyStartupState();
            m_RuntimeStarted = true;
        }

        void Update()
        {
            if (!m_RuntimeStarted)
                return;

            m_Cursor.Tick();
            m_Timer.Tick(Time.deltaTime);
        }

        void OnApplicationFocus(bool focus)
        {
            if (!m_RuntimeStarted)
                return;

            m_Cursor.HandleApplicationFocus(focus);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(m_Cursor);
            ServiceLocator.Unregister(m_Timer);
        }
    }
}
