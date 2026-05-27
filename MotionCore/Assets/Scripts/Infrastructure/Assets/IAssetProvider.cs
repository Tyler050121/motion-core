using UnityEngine;

namespace MotionCore.Infrastructure
{
    /// <summary>
    /// 运行时资源访问接口。
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// 按 key 加载 Unity 资源。
        /// </summary>
        T Load<T>(string key) where T : Object;

        /// <summary>
        /// 按 key 加载 prefab，并返回 prefab 上的指定组件。
        /// </summary>
        T LoadComponent<T>(string key) where T : Component;
    }
}
