using System;
using UnityEngine;

namespace MotionCore.Infrastructure
{
    /// <summary>
    /// 基于 Resources 的资源访问实现。
    /// </summary>
    public sealed class ResourcesAssetProvider : IAssetProvider
    {
        public T Load<T>(string key) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("资源 key 不能为空。", nameof(key));

            key = key.Replace('\\', '/');
            T asset = Resources.Load<T>(key);
            if (!asset)
                throw new InvalidOperationException($"Resources 未找到资源：{key} ({typeof(T).Name})");

            return asset;
        }

        public T LoadComponent<T>(string key) where T : Component
        {
            GameObject prefab = Load<GameObject>(key);
            T component = prefab.GetComponent<T>();
            if (!component)
                throw new InvalidOperationException($"Resources prefab 缺少组件：{key} ({typeof(T).Name})");

            return component;
        }
    }
}
