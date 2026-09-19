using System.IO;
using MotionCore.Gameplay.Character;
using MotionCore.Gameplay.Configs;
using MotionCore.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [SerializeField, ShowIf("m_AssetProviderMode", AssetProviderMode.YooAsset), Tooltip("YooAsset 收集资源根目录")]
        string m_YooAssetAssetRoot = YooAssetProviderSettings.DefaultAssetRoot;

        [SerializeField, ShowIf("m_AssetProviderMode", AssetProviderMode.YooAsset),
         Tooltip("编辑器下的 YooAsset 播放模式，Player 固定使用 Offline")]
        YooAssetPlayMode m_YooAssetPlayMode = YooAssetPlayMode.EditorSimulate;

        [SerializeField, Tooltip("运行时实例根节点")]
        Transform m_RuntimeRoot;

        [SerializeField, Tooltip("启动完成后进入的场景")]
        string m_StartupSceneName;

        ICursorService m_Cursor;
        ITimerService m_Timer;
        IGameTimeService m_GameTime;
        IEventBus m_EventBus;
        IAssetProvider m_Assets;
        IConfigProvider m_Configs;
        CharacterSpawner m_CharacterSpawner;
        AudioService m_Audio;
        ISaveService m_Save;
        VfxService m_Vfx;
        SceneNavigator m_SceneNavigator;
        UIRuntimeBootstrap m_UiBootstrap;
        bool m_RuntimeStarted;

        void Awake()
        {
            // 初始化运行时基础服务，并交由 UI 启动器启动界面。
            Application.runInBackground = GlobalConfig.Application.RunInBackground;
            DontDestroyOnLoad(gameObject);
            m_Cursor = new CursorService();
            m_Timer = new TimerService();
            m_GameTime = new GameTimeService();
            m_EventBus = new EventBus();
            m_Assets = CreateAssetProvider();
            m_Audio = new AudioService(m_Assets, m_RuntimeRoot);
            m_Save = new JsonSaveService(Path.Combine(Application.persistentDataPath, "Saves"));
            m_Vfx = new VfxService(m_Assets, m_Timer, m_RuntimeRoot);
            m_SceneNavigator = new SceneNavigator(m_EventBus);
            m_UiBootstrap = GetComponent<UIRuntimeBootstrap>();
            ServiceLocator.Register(m_Cursor);
            ServiceLocator.Register(m_Timer);
            ServiceLocator.Register(m_GameTime);
            ServiceLocator.Register(m_EventBus);
            ServiceLocator.Register(m_Assets);
            ServiceLocator.Register<IAudioService>(m_Audio);
            ServiceLocator.Register(m_Save);
            ServiceLocator.Register<IVfxService>(m_Vfx);
            ServiceLocator.Register<ISceneNavigator>(m_SceneNavigator);
        }

        async void Start()
        {
            m_Cursor.ApplyStartupState();
            if (m_Assets is YooAssetProvider yooAssetProvider)
            {
                // YooAsset 模式需在资源包初始化完成后启动 UI。
                await yooAssetProvider.InitializationTask;
            }

            // 配置表加载依赖资源提供器，需在 YooAsset 初始化完成后执行。
            m_Configs = CreateConfigProvider();
            ServiceLocator.Register<IConfigProvider>(m_Configs);
            m_CharacterSpawner = new CharacterSpawner(m_Configs, m_Assets);
            ServiceLocator.Register(m_CharacterSpawner);

            m_UiBootstrap.Boot();
            m_RuntimeStarted = true;
            await m_SceneNavigator.LoadScene(m_StartupSceneName, LoadSceneMode.Single);
        }

        void Update()
        {
            if (!m_RuntimeStarted)
                return;

            m_Cursor.Tick();
            m_Timer.Tick(Time.deltaTime);
            m_GameTime.Tick(Time.unscaledDeltaTime);
            m_Audio.Tick();
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
            ServiceLocator.Unregister(m_GameTime);
            ServiceLocator.Unregister(m_EventBus);
            ServiceLocator.Unregister(m_Assets);
            ServiceLocator.Unregister<IConfigProvider>(m_Configs);
            ServiceLocator.Unregister(m_CharacterSpawner);
            ServiceLocator.Unregister<IAudioService>(m_Audio);
            ServiceLocator.Unregister(m_Save);
            ServiceLocator.Unregister<IVfxService>(m_Vfx);
            ServiceLocator.Unregister<ISceneNavigator>(m_SceneNavigator);
            m_SceneNavigator.Dispose();
            m_EventBus.Clear();
            m_Vfx.Dispose();
            m_Audio.Dispose();
            m_GameTime.Reset();
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

        IConfigProvider CreateConfigProvider()
        {
            LubanBinaryConfigLoader loader = new(m_Assets);
            Tables tables = new(loader.Load);

            return new RuntimeConfigProvider(tables.GetAllTables());
        }

        YooAssetProvider CreateYooAssetProvider()
        {
            return new YooAssetProvider(new YooAssetProviderSettings
            {
                PackageName = m_YooAssetPackageName,
                AssetRoot = m_YooAssetAssetRoot,
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
