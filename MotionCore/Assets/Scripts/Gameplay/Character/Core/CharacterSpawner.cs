using MotionCore.Gameplay.Configs;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 根据角色生成表创建并初始化角色。
    /// </summary>
    public sealed class CharacterSpawner
    {
        readonly IConfigProvider m_Configs;
        readonly IAssetProvider m_Assets;

        public CharacterSpawner(IConfigProvider configs, IAssetProvider assets)
        {
            m_Configs = configs;
            m_Assets = assets;
        }

        /// <summary>
        /// 按生成记录 ID 创建角色。
        /// </summary>
        public Character Spawn(int spawnId, Transform parent = null)
        {
            CharacterSpawn spawn = m_Configs.GetTable<TbCharacterSpawn>().Get(spawnId);
            CharacterRow character = spawn.CharacterId_Ref;
            CharacterStat stat = character.StatId_Ref;
            Character prefab = m_Assets.LoadComponent<Character>(character.ResKey);

            Character instance = UnityEngine.Object.Instantiate(
                prefab,
                new Vector3(spawn.PosX, spawn.PosY, spawn.PosZ),
                Quaternion.Euler(0f, spawn.Yaw, 0f),
                parent);
            instance.Initialize(stat);
            return instance;
        }
    }
}
