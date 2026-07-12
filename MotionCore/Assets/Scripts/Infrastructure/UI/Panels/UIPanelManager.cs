using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MotionCore.Infrastructure
{
    /// <summary>
    /// 统一 UI 生命周期入口：按需实例化面板并通过显示/隐藏切换。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIPanelManager : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("统一 UI 系统配置")]
        [SerializeField]
        UISystemConfig m_Config;

        RectTransform m_Root;
        readonly Dictionary<string, UIPanelHandle> m_LoadedPanels = new Dictionary<string, UIPanelHandle>();
        readonly HashSet<string> m_LoadingPanels = new HashSet<string>();
        readonly HashSet<string> m_PendingOpenPanels = new HashSet<string>();
        readonly HashSet<string> m_AllowedPanelIDs = new HashSet<string>();
        readonly Dictionary<string, RectTransform> m_LayerRoots = new Dictionary<string, RectTransform>();
        readonly List<string> m_BackStack = new List<string>();
        readonly List<string> m_ReleaseBuffer = new List<string>(16);
        readonly List<string> m_LoadedPanelIDBuffer = new List<string>(16);

        IAssetProvider m_AssetProvider;
        bool m_RuntimeInitialized;

        public UISystemConfig Config => m_Config;

        public void SetConfig(UISystemConfig config)
        {
            if (m_RuntimeInitialized)
            {
                Debug.LogWarning("[UIPanelManager] config cannot be changed after runtime initialization.");
                return;
            }

            m_Config = config;
        }

        public void InitializeRuntime()
        {
            if (m_RuntimeInitialized)
            {
                return;
            }

            if (m_Config == null)
            {
                Debug.LogError("[UIPanelManager] UISystemConfig is missing.");
                return;
            }

            EnsureRoot();
            EnsureLayerRoots();
            RefreshCanvasSettings();
            m_RuntimeInitialized = true;
        }

        /// <summary>
        /// 刷新各层 Canvas 参数，保证切换场景后仍然使用正确的相机和缩放规则。
        /// </summary>
        public void RefreshCanvasSettings()
        {
            if (m_Config == null)
            {
                return;
            }

            Camera renderCamera = ResolveRenderCamera();
            foreach (KeyValuePair<string, RectTransform> pair in m_LayerRoots)
            {
                Canvas canvas = pair.Value.GetComponent<Canvas>();
                CanvasScaler scaler = pair.Value.GetComponent<CanvasScaler>();
                ApplyCanvasSettings(canvas, scaler, renderCamera);
            }
        }

        public RectTransform GetLayerRoot(string layerId)
        {
            if (string.IsNullOrWhiteSpace(layerId))
            {
                return null;
            }

            m_LayerRoots.TryGetValue(layerId, out RectTransform root);
            return root;
        }

        public bool TryGetWidgetEntry(string widgetId, out UISystemConfig.UIWidgetEntry entry)
        {
            entry = null;
            return m_Config != null && m_Config.TryGetWidget(widgetId, out entry);
        }

        public void PreloadPanels(IReadOnlyCollection<string> panelIDs)
        {
            if (!m_RuntimeInitialized)
            {
                return;
            }

            foreach (string panelID in panelIDs)
            {
                PreloadPanel(panelID);
            }
        }

        public void SetAllowedPanels(IReadOnlyCollection<string> panelIDs)
        {
            m_AllowedPanelIDs.Clear();
            foreach (string panelID in panelIDs)
            {
                m_AllowedPanelIDs.Add(panelID);
            }
        }

        public bool IsAllowed(string panelID)
        {
            return m_AllowedPanelIDs.Contains(panelID);
        }

        public void ReleasePanelsExcept(HashSet<string> keepPanelIDs)
        {
            ResetRuntimeOperations();
            CollectReleasablePanels(keepPanelIDs);

            for (int i = 0; i < m_ReleaseBuffer.Count; i++)
            {
                ReleasePanel(m_ReleaseBuffer[i]);
            }
        }

        public bool IsOpen(string panelID)
        {
            return m_LoadedPanels.TryGetValue(panelID, out UIPanelHandle handle) && handle.Instance &&
                   handle.Instance.activeSelf;
        }

        public bool TryGetPanelComponent<T>(string panelID, out T component)
            where T : Component
        {
            component = null;
            if (!m_LoadedPanels.TryGetValue(panelID, out UIPanelHandle handle) || !handle.Instance)
            {
                return false;
            }

            component = handle.Instance.GetComponent<T>();
            return component != null;
        }

        public void Open(string panelID)
        {
            if (!CanOpenPanel(panelID))
            {
                return;
            }

            if (!TryGetLoadedHandle(panelID, out UIPanelHandle handle))
            {
                QueueOpenAfterLoad(panelID);
                return;
            }

            if (handle.IsClosing)
            {
                handle.PendingShow = true;
                return;
            }

            if (!CanShow(handle))
            {
                return;
            }

            StartPanelTransition(handle, ShowRoutine(handle));
        }

        public void Close(string panelID)
        {
            if (!m_RuntimeInitialized)
            {
                return;
            }

            m_PendingOpenPanels.Remove(panelID);

            if (!TryGetLoadedHandle(panelID, out UIPanelHandle handle))
            {
                return;
            }

            handle.PendingShow = false;

            if (!CanHide(handle))
            {
                return;
            }

            StartPanelTransition(handle, HideRoutine(handle));
        }

        public void CloseAll()
        {
            m_LoadedPanelIDBuffer.Clear();
            foreach (string panelID in m_LoadedPanels.Keys)
            {
                m_LoadedPanelIDBuffer.Add(panelID);
            }

            for (int i = 0; i < m_LoadedPanelIDBuffer.Count; i++)
            {
                Close(m_LoadedPanelIDBuffer[i]);
            }
        }

        public bool TryCloseTopEscapable()
        {
            for (int i = m_BackStack.Count - 1; i >= 0; i--)
            {
                string panelID = m_BackStack[i];
                if (!TryGetLoadedHandle(panelID, out UIPanelHandle handle))
                {
                    m_BackStack.RemoveAt(i);
                    continue;
                }

                if (!CanCloseFromBackStack(handle))
                {
                    continue;
                }

                Close(panelID);
                return true;
            }

            return false;
        }

        void NormalizeLoadedPanelStates()
        {
            foreach (KeyValuePair<string, UIPanelHandle> pair in m_LoadedPanels)
            {
                UIPanelHandle handle = pair.Value;
                handle.IsClosing = false;
                handle.PendingShow = false;
                handle.TransitionRoutine = null;
                if (!handle.Instance)
                {
                    continue;
                }

                ApplyVisibleState(handle.Instance, handle.Instance.activeSelf);
            }
        }

        void PreloadPanel(string panelID)
        {
            if (m_LoadedPanels.ContainsKey(panelID) || m_LoadingPanels.Contains(panelID))
            {
                return;
            }

            LoadPanel(panelID);
        }

        /// <summary>
        /// 开始加载面板并避免重复并发。
        /// </summary>
        void LoadPanel(string panelID)
        {
            m_LoadingPanels.Add(panelID);
            CreatePanel(panelID);
            m_LoadingPanels.Remove(panelID);
            TryOpenPendingPanel(panelID);
        }

        /// <summary>
        /// 从配置、层级和资源三步创建面板实例。
        /// </summary>
        void CreatePanel(string panelID)
        {
            if (!TryGetPanelEntry(panelID, out UISystemConfig.UIPanelEntry entry))
            {
                return;
            }

            RectTransform parent = GetLayerRoot(entry.LayerId);
            if (parent == null)
            {
                Debug.LogError("[UIPanelManager] layer not found: " + entry.LayerId + " panel=" + panelID);
                return;
            }

            GameObject prefab = LoadPanelPrefab(entry.AssetKey, panelID);
            if (!prefab)
            {
                return;
            }

            GameObject instance = Instantiate(prefab, parent, false);
            RegisterLoadedPanel(panelID, entry, instance);
        }

        /// <summary>
        /// 显示面板并处理进入动画。
        /// </summary>
        System.Collections.IEnumerator ShowRoutine(UIPanelHandle handle)
        {
            handle.IsClosing = false;
            handle.PendingShow = false;
            ApplyVisibleState(handle.Instance, true);
            handle.Instance.SetActive(true);

            UIPanelTransition transition = handle.Instance.GetComponent<UIPanelTransition>();
            if (transition)
            {
                transition.PrepareForOpen();
                yield return transition.PlayOpen();
            }

            if (handle.IsEscapable && !m_BackStack.Contains(handle.PanelID))
            {
                m_BackStack.Add(handle.PanelID);
            }

            handle.TransitionRoutine = null;
        }

        /// <summary>
        /// 关闭面板并处理退出动画。
        /// </summary>
        System.Collections.IEnumerator HideRoutine(UIPanelHandle handle)
        {
            handle.IsClosing = true;
            handle.PendingShow = false;

            UIPanelTransition transition = handle.Instance.GetComponent<UIPanelTransition>();
            if (transition)
            {
                yield return transition.PlayClose();
            }

            ApplyVisibleState(handle.Instance, false);
            handle.Instance.SetActive(false);
            m_BackStack.Remove(handle.PanelID);
            handle.IsClosing = false;
            handle.TransitionRoutine = null;

            if (handle.PendingShow && CanOpenPanel(handle.PanelID))
            {
                StartPanelTransition(handle, ShowRoutine(handle));
            }
        }

        bool TryGetPanelEntry(string panelID, out UISystemConfig.UIPanelEntry entry)
        {
            entry = null;
            if (m_Config == null || !m_Config.TryGetPanel(panelID, out entry))
            {
                Debug.LogError("[UIPanelManager] panel not registered: " + panelID);
                return false;
            }

            return true;
        }

        void ResetRuntimeOperations()
        {
            StopAllCoroutines();
            NormalizeLoadedPanelStates();
            m_PendingOpenPanels.Clear();
            m_LoadingPanels.Clear();
        }

        void CollectReleasablePanels(HashSet<string> keepPanelIDs)
        {
            m_ReleaseBuffer.Clear();
            foreach (KeyValuePair<string, UIPanelHandle> pair in m_LoadedPanels)
            {
                if (keepPanelIDs.Contains(pair.Key))
                {
                    continue;
                }

                m_ReleaseBuffer.Add(pair.Key);
            }
        }

        void ReleasePanel(string panelID)
        {
            UIPanelHandle handle = m_LoadedPanels[panelID];
            if (handle.TransitionRoutine != null)
            {
                StopCoroutine(handle.TransitionRoutine);
                handle.TransitionRoutine = null;
            }

            m_BackStack.Remove(panelID);
            m_LoadedPanels.Remove(panelID);
            Destroy(handle.Instance);
        }

        bool TryGetLoadedHandle(string panelID, out UIPanelHandle handle)
        {
            return m_LoadedPanels.TryGetValue(panelID, out handle);
        }

        bool CanOpenPanel(string panelID)
        {
            return m_RuntimeInitialized && m_AllowedPanelIDs.Contains(panelID);
        }

        static bool CanShow(UIPanelHandle handle)
        {
            return handle.Instance && !handle.Instance.activeSelf && !handle.IsClosing;
        }

        static bool CanHide(UIPanelHandle handle)
        {
            return handle.Instance && handle.Instance.activeSelf && !handle.IsClosing;
        }

        static bool CanCloseFromBackStack(UIPanelHandle handle)
        {
            return CanHide(handle) && handle.IsEscapable;
        }

        void QueueOpenAfterLoad(string panelID)
        {
            m_PendingOpenPanels.Add(panelID);
            if (m_LoadingPanels.Contains(panelID))
            {
                return;
            }

            LoadPanel(panelID);
        }

        void TryOpenPendingPanel(string panelID)
        {
            if (!m_PendingOpenPanels.Remove(panelID))
            {
                return;
            }

            Open(panelID);
        }

        /// <summary>
        /// 将面板实例登记到运行时缓存中。
        /// </summary>
        void RegisterLoadedPanel(string panelID, UISystemConfig.UIPanelEntry entry, GameObject instance)
        {
            instance.name = panelID;
            EnsureCanvasGroup(instance);
            ApplyHiddenState(instance);

            m_LoadedPanels[panelID] = new UIPanelHandle
            {
                PanelID = panelID,
                Instance = instance,
                IsClosing = false,
                PendingShow = false,
                IsEscapable = entry.IsEscapable
            };
        }

        /// <summary>
        /// 创建 UI 根节点。
        /// </summary>
        void EnsureRoot()
        {
            if (m_Root)
            {
                return;
            }

            string rootName = string.IsNullOrWhiteSpace(m_Config.Root.RootName)
                                  ? "UIRoot"
                                  : m_Config.Root.RootName;
            GameObject rootObject = new GameObject(rootName, typeof(RectTransform));
            rootObject.transform.SetParent(transform, false);
            m_Root = rootObject.GetComponent<RectTransform>();
            StretchRoot(m_Root);
        }

        /// <summary>
        /// 创建各个 UI 层级根节点。
        /// </summary>
        void EnsureLayerRoots()
        {
            for (int i = 0; i < m_Config.Layers.Count; i++)
            {
                UISystemConfig.LayerEntry layer = m_Config.Layers[i];
                if (layer == null || string.IsNullOrWhiteSpace(layer.LayerId) ||
                    m_LayerRoots.ContainsKey(layer.LayerId))
                {
                    continue;
                }

                GameObject layerObject = new GameObject(
                    UISystemConfig.DefaultLayerPrefix + layer.LayerId,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler));
                if (layer.HasGraphicRaycaster)
                {
                    layerObject.AddComponent<GraphicRaycaster>();
                }

                layerObject.transform.SetParent(m_Root, false);
                RectTransform layerRoot = layerObject.GetComponent<RectTransform>();
                StretchRoot(layerRoot);
                ConfigureLayerCanvas(layerObject, layer);
                m_LayerRoots.Add(layer.LayerId, layerRoot);
            }
        }

        void ConfigureLayerCanvas(GameObject layerRoot, UISystemConfig.LayerEntry layer)
        {
            Canvas canvas = layerRoot.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = layer.SortingOrder;

            CanvasScaler scaler = layerRoot.GetComponent<CanvasScaler>();
            ApplyCanvasSettings(canvas, scaler, ResolveRenderCamera());
        }

        void ApplyCanvasSettings(Canvas canvas, CanvasScaler scaler, Camera renderCamera)
        {
            bool useCamera = m_Config.Root.RenderMode == UISystemConfig.UIRenderMode.ScreenSpaceCamera;
            canvas.renderMode = useCamera ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = useCamera ? renderCamera : null;
            canvas.planeDistance = m_Config.Root.PlaneDistance;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = m_Config.Root.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = m_Config.Root.MatchWidthOrHeight;
        }

        Camera ResolveRenderCamera()
        {
            if (m_Config.Root.CameraResolveMode == UISystemConfig.UICameraResolveMode.Tag &&
                !string.IsNullOrWhiteSpace(m_Config.Root.CameraTag))
            {
                GameObject taggedObject = GameObject.FindWithTag(m_Config.Root.CameraTag);
                return taggedObject ? taggedObject.GetComponent<Camera>() : null;
            }

            return Camera.main;
        }

        static void StretchRoot(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
        }

        static void ApplyHiddenState(GameObject panel)
        {
            ApplyVisibleState(panel, false);
            panel.SetActive(false);
        }

        static void ApplyVisibleState(GameObject panel, bool visible)
        {
            CanvasGroup group = EnsureCanvasGroup(panel);
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        static CanvasGroup EnsureCanvasGroup(GameObject panel)
        {
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (!group)
            {
                group = panel.AddComponent<CanvasGroup>();
            }

            return group;
        }

        void StartPanelTransition(UIPanelHandle handle, IEnumerator routine)
        {
            if (handle.TransitionRoutine != null)
            {
                StopCoroutine(handle.TransitionRoutine);
            }

            handle.TransitionRoutine = StartCoroutine(routine);
        }

        IAssetProvider GetAssetProvider()
        {
            m_AssetProvider ??= ServiceLocator.Resolve<IAssetProvider>();
            return m_AssetProvider;
        }

        /// <summary>
        /// 直接从资源提供器同步加载面板预制体。
        /// </summary>
        GameObject LoadPanelPrefab(string assetKey, string panelId)
        {
            IAssetProvider assetProvider = GetAssetProvider();
            if (assetProvider == null)
            {
                Debug.LogError("[UIPanelManager] IAssetProvider is missing.");
                return null;
            }

            return assetProvider.Load<GameObject>(assetKey);
        }
    }
}
