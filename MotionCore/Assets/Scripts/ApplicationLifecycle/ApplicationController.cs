using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.ApplicationLifecycle
{
    /// <summary>
    /// 游戏应用生命周期入口。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIRuntimeBootstrap))]
    public sealed class ApplicationController : MonoBehaviour
    {
        enum AssetProviderMode
        {
            Resources = 0,
            YooAsset = 1
        }

        [SerializeField, Tooltip("运行时资源提供方式")]
        AssetProviderMode m_AssetProviderMode = AssetProviderMode.YooAsset;

        [SerializeField, ShowIf("m_AssetProviderMode", AssetProviderMode.YooAsset), Tooltip("YooAsset 包名")]
        string m_YooAssetPackageName = YooAssetProviderSettings.DefaultPackageName;

        [SerializeField, ShowIf("m_AssetProviderMode", AssetProviderMode.YooAsset),
         Tooltip("编辑器下的 YooAsset 播放模式，Player 固定使用 Offline")]
        YooAssetPlayMode m_YooAssetPlayMode = YooAssetPlayMode.EditorSimulate;

        [SerializeField, Tooltip("VFX 根节点")]
        Transform m_VfxRoot;

        ICursorService m_Cursor;
        ITimerService m_Timer;
        IAssetProvider m_Assets;
        VfxService m_Vfx;
        UIRuntimeBootstrap m_UiBootstrap;
        bool m_RuntimeStarted;

        void Awake()
        {
            // 先把运行时基础服务准备好，再交给 UI 启动器接管界面。
            Application.runInBackground = GlobalConfig.Application.RunInBackground;
            m_Cursor = new CursorService();
            m_Timer = new TimerService();
            m_Assets = CreateAssetProvider();
            m_Vfx = new VfxService(m_Assets, m_Timer, m_VfxRoot);
            m_UiBootstrap = GetComponent<UIRuntimeBootstrap>();
            ServiceLocator.Register(m_Cursor);
            ServiceLocator.Register(m_Timer);
            ServiceLocator.Register(m_Assets);
            ServiceLocator.Register<IVfxService>(m_Vfx);
        }

        async void Start()
        {
            m_Cursor.ApplyStartupState();
            if (m_Assets is YooAssetProvider yooAssetProvider)
            {
                // YooAsset 场景下，先等资源侧初始化完成再启动 UI。
                await yooAssetProvider.InitializationTask;
            }

            m_UiBootstrap.Boot();

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
            m_UiBootstrap.Shutdown();

            ServiceLocator.Unregister(m_Cursor);
            ServiceLocator.Unregister(m_Timer);
            ServiceLocator.Unregister(m_Assets);
            ServiceLocator.Unregister<IVfxService>(m_Vfx);
            m_Vfx.Dispose();
            if (m_Assets is System.IDisposable disposableAssets)
            {
                disposableAssets.Dispose();
            }
        }

        IAssetProvider CreateAssetProvider()
        {
            if (m_AssetProviderMode == AssetProviderMode.YooAsset)
            {
                return CreateYooAssetProvider();
            }

            return new ResourcesAssetProvider();
        }

        YooAssetProvider CreateYooAssetProvider()
        {
            return new YooAssetProvider(new YooAssetProviderSettings
            {
                PackageName = m_YooAssetPackageName,
                PlayMode = GetYooAssetPlayMode()
            });
        }

        YooAssetPlayMode GetYooAssetPlayMode()
        {
#if UNITY_EDITOR
            return m_YooAssetPlayMode;
#else
            return YooAssetPlayMode.Offline;
#endif
        }
    }
}
