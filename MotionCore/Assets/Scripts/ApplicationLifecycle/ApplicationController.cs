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
        [SerializeField, Tooltip("VFX 根节点")]
        Transform m_VfxRoot;

        ICursorService m_Cursor;
        ITimerService m_Timer;
        IAssetProvider m_Assets;
        VfxService m_Vfx;
        bool m_RuntimeStarted;

        void Awake()
        {
            Application.runInBackground = GlobalConfig.Application.RunInBackground;
            m_Cursor = new CursorService();
            m_Timer = new TimerService();
            m_Assets = new ResourcesAssetProvider();
            m_Vfx = new VfxService(m_Assets, m_Timer, m_VfxRoot);
            ServiceLocator.Register(m_Cursor);
            ServiceLocator.Register(m_Timer);
            ServiceLocator.Register(m_Assets);
            ServiceLocator.Register<IVfxService>(m_Vfx);
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
            ServiceLocator.Unregister(m_Assets);
            ServiceLocator.Unregister<IVfxService>(m_Vfx);
            m_Vfx.Dispose();
        }
    }
}
