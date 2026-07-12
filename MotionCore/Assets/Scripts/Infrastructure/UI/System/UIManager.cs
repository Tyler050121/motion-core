using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MotionCore.Infrastructure
{
    /// <summary>
    /// UI 子系统主入口：根据当前场景作用域收敛允许面板与默认打开面板。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIPanelManager))]
    public sealed class UIManager : MonoBehaviour
    {
        [Header("流程")]
        [Tooltip("按 ESC 时尝试关闭顶部可返回面板")]
        [SerializeField]
        bool m_EnableEscapeBack = true;

        readonly HashSet<string> m_KeepPanelIds = new();
        readonly HashSet<string> m_PreloadPanelIds = new();
        readonly HashSet<string> m_OpenOnEnterPanelIds = new();

        UIPanelManager m_PanelManager;
        UIService m_UIService;
        bool m_Booted;

        UISystemConfig Config => m_PanelManager.Config;

        void Awake()
        {
            m_PanelManager = GetComponent<UIPanelManager>();
        }

        /// <summary>
        /// 退出时注销 UI 服务与场景监听。
        /// </summary>
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ServiceLocator.Unregister(m_PanelManager);
            if (m_UIService != null)
            {
                ServiceLocator.Unregister<IUIService>(m_UIService);
            }
        }

        void Update()
        {
            if (!m_EnableEscapeBack)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                m_PanelManager.TryCloseTopEscapable();
        }

        /// <summary>
        /// 初始化面板运行时并应用当前场景作用域。
        /// </summary>
        public void BootRuntime()
        {
            if (m_Booted)
                return;

            m_PanelManager.InitializeRuntime();
            ServiceLocator.Register(m_PanelManager);
            m_UIService = new UIService(m_PanelManager);
            ServiceLocator.Register<IUIService>(m_UIService);
            ApplySceneScope(SceneManager.GetActiveScene().name, false);
            SceneManager.sceneLoaded += OnSceneLoaded;
            m_Booted = true;
        }

        /// <summary>
        /// 场景切换后重新应用 UI 作用域。
        /// </summary>
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            m_PanelManager.RefreshCanvasSettings();
            ApplySceneScope(scene.name, true);
        }

        /// <summary>
        /// 按场景作用域更新允许、预加载和默认打开的面板。
        /// </summary>
        void ApplySceneScope(string sceneName, bool releaseUnscopedPanels)
        {
            string scopeId = Config.TryGetSceneScope(sceneName, out string mappedScopeId)
                                 ? mappedScopeId
                                 : UISystemConfig.DefaultScopeIds.None;
            m_KeepPanelIds.Clear();
            m_PreloadPanelIds.Clear();
            m_OpenOnEnterPanelIds.Clear();
            AddPanelSelections(Config.GlobalPanels);

            if (Config.TryGetScopePanelGroup(scopeId, out UISystemConfig.ScopePanelGroup group))
                AddPanelSelections(group.Panels);

            m_PanelManager.SetAllowedPanels(m_KeepPanelIds);
            if (releaseUnscopedPanels)
                m_PanelManager.ReleasePanelsExcept(m_KeepPanelIds);

            m_PanelManager.PreloadPanels(m_PreloadPanelIds);

            foreach (string panelId in m_KeepPanelIds)
            {
                if (m_OpenOnEnterPanelIds.Contains(panelId))
                    m_PanelManager.Open(panelId);
                else
                    m_PanelManager.Close(panelId);
            }
        }

        void AddPanelSelections(IReadOnlyList<UISystemConfig.ScopePanelSelection> panelConfigs)
        {
            for (int i = 0; i < panelConfigs.Count; i++)
            {
                UISystemConfig.ScopePanelSelection panelConfig = panelConfigs[i];
                m_KeepPanelIds.Add(panelConfig.PanelId);
                if (panelConfig.Preload)
                    m_PreloadPanelIds.Add(panelConfig.PanelId);

                if (panelConfig.OpenOnEnter)
                    m_OpenOnEnterPanelIds.Add(panelConfig.PanelId);
            }
        }
    }
}
