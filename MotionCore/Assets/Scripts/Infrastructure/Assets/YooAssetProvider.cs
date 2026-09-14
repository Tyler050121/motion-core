using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace MotionCore.Infrastructure
{
    /// <summary>
    /// 基于 YooAsset 的运行时资源提供器。
    /// </summary>
    public sealed class YooAssetProvider : IAssetProvider, IDisposable
    {
        readonly Dictionary<string, AssetHandle> m_LoadedHandles = new(StringComparer.Ordinal);
        readonly ResourcePackage m_Package;
        bool m_IsInitialized;

        /// <summary>
        /// 创建并开始初始化独占的 YooAsset 运行时。
        /// </summary>
        public YooAssetProvider(YooAssetProviderSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.PackageName))
                throw new ArgumentException("YooAsset 包名不能为空。", nameof(settings.PackageName));
            if (YooAssets.Initialized)
                throw new InvalidOperationException("YooAsset 运行时已经初始化。");

            YooAssets.Initialize();
            m_Package = YooAssets.CreatePackage(settings.PackageName);
            InitializationTask = InitializeAsync(settings.PlayMode);
        }

        /// <summary>
        /// 资源初始化任务。
        /// </summary>
        public Task InitializationTask { get; }

        /// <summary>
        /// 同步加载资源并持有对应句柄。
        /// </summary>
        public T Load<T>(string key)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("资源 key 不能为空。", nameof(key));

            if (!m_IsInitialized)
                throw new InvalidOperationException("YooAsset 尚未初始化完成，请先等待 InitializationTask。");

            key = key.Replace('\\', '/');
            string cacheKey = typeof(T).FullName + ":" + key;
            if (!m_LoadedHandles.TryGetValue(cacheKey, out AssetHandle handle))
            {
                handle = m_Package.LoadAssetAsync<T>(key);
                handle.WaitForAsyncComplete();
                if (handle.Status != EOperationStatus.Succeed)
                {
                    string error = handle.LastError;
                    handle.Release();
                    throw new InvalidOperationException($"YooAsset 加载资源失败：{key} ({typeof(T).Name})，{error}");
                }

                m_LoadedHandles.Add(cacheKey, handle);
            }

            T asset = handle.GetAssetObject<T>();
            if (!asset)
                throw new InvalidOperationException($"YooAsset 未找到资源：{key} ({typeof(T).Name})");

            return asset;
        }

        /// <summary>
        /// 同步加载 prefab 上的指定组件。
        /// </summary>
        public T LoadComponent<T>(string key)
            where T : Component
        {
            GameObject prefab = Load<GameObject>(key);
            T component = prefab.GetComponent<T>();
            if (!component)
                throw new InvalidOperationException($"YooAsset prefab 缺少组件：{key} ({typeof(T).Name})");

            return component;
        }

        /// <summary>
        /// 释放资源句柄并销毁 YooAsset 运行时。
        /// </summary>
        public void Dispose()
        {
            foreach (KeyValuePair<string, AssetHandle> pair in m_LoadedHandles)
                pair.Value.Release();

            m_LoadedHandles.Clear();
            YooAssets.Destroy();
        }

        /// <summary>
        /// 依次初始化资源包、读取版本并加载资源清单。
        /// </summary>
        async Task InitializeAsync(YooAssetPlayMode playMode)
        {
            InitializationOperation initializeOperation;
#if UNITY_EDITOR
            if (playMode == YooAssetPlayMode.EditorSimulate)
            {
                PackageInvokeBuildResult buildResult = EditorSimulateModeHelper.SimulateBuild(m_Package.PackageName);
                var parameters = new EditorSimulateModeParameters
                {
                    EditorFileSystemParameters =
                        FileSystemParameters.CreateDefaultEditorFileSystemParameters(
                            buildResult.PackageRootDirectory)
                };
                initializeOperation = m_Package.InitializeAsync(parameters);
            }
            else
#endif
            if (playMode == YooAssetPlayMode.Offline)
            {
                var parameters = new OfflinePlayModeParameters
                {
                    BuildinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                };
                initializeOperation = m_Package.InitializeAsync(parameters);
            }
            else
            {
                throw new InvalidOperationException($"当前平台不支持 YooAsset 播放模式：{playMode}");
            }

            await initializeOperation.Task;
            if (initializeOperation.Status != EOperationStatus.Succeed)
                throw new InvalidOperationException($"YooAsset 初始化资源包失败：{initializeOperation.Error}");

            RequestPackageVersionOperation versionOperation = m_Package.RequestPackageVersionAsync();
            await versionOperation.Task;
            if (versionOperation.Status != EOperationStatus.Succeed)
                throw new InvalidOperationException($"YooAsset 读取资源版本失败：{versionOperation.Error}");

            UpdatePackageManifestOperation manifestOperation =
                m_Package.UpdatePackageManifestAsync(versionOperation.PackageVersion);
            await manifestOperation.Task;
            if (manifestOperation.Status != EOperationStatus.Succeed)
                throw new InvalidOperationException($"YooAsset 加载资源清单失败：{manifestOperation.Error}");

            m_IsInitialized = true;
        }
    }
}
