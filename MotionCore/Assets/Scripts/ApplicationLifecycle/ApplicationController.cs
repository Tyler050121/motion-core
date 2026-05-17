using MotionCore.Bootstrap;
using MotionCore.Gameplay;
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
        bool m_RuntimeStarted;

        void Awake()
        {
            Application.runInBackground = GlobalConfig.Application.RunInBackground;
            m_Cursor = new CursorService();
            ServiceLocator.Register(m_Cursor);
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
        }
    }
}
