using System;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 创建场景声明的角色实例。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterSceneBootstrap : MonoBehaviour
    {
        [SerializeField, Tooltip("角色生成记录 ID")]
        int[] m_SpawnIds = Array.Empty<int>();

        void Start()
        {
            CharacterSpawner spawner = ServiceLocator.Resolve<CharacterSpawner>();
            for (int i = 0; i < m_SpawnIds.Length; i++)
                spawner.Spawn(m_SpawnIds[i], transform);
        }
    }
}
