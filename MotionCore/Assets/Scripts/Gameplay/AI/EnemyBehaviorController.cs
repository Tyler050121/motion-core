using MotionCore.Gameplay.Character;
using UnityEngine;
using CharacterContext = MotionCore.Gameplay.Character.Character;

namespace MotionCore.Gameplay.AI
{
    /// <summary>
    /// 敌人行为的集成入口。持有角色引用与行为参数，向行为树节点暴露最小移动原语。
    /// 不承载任何行为决策——巡逻/追击等逻辑由行为树节点编排，本类只负责把意图下发给命令控制器。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyBehaviorController : MonoBehaviour
    {
        [SerializeField, Tooltip("敌人角色根")]
        CharacterContext m_Character;
        [SerializeField, Tooltip("敌人行为参数")]
        EnemyBehaviorConfig m_BehaviorConfig;

        ICharacterCommandExecutor m_CommandExecutor;
        Vector3 m_HomePosition;

        /// <summary>
        /// 敌人当前世界坐标。
        /// </summary>
        public Vector3 Position => m_Character.transform.position;

        /// <summary>
        /// 出生点，用作巡逻锚点。
        /// </summary>
        public Vector3 HomePosition => m_HomePosition;

        /// <summary>
        /// 行为参数配置。
        /// </summary>
        public EnemyBehaviorConfig Config => m_BehaviorConfig;

        void Awake()
        {
            m_CommandExecutor = GetComponentInChildren<ICharacterCommandExecutor>();
            m_HomePosition = m_Character.transform.position;
        }

        void OnDisable()
        {
            m_CommandExecutor.StopMove();
        }

        /// <summary>
        /// 朝目标方向前进，身体边走边转向目标（走出弧线），无需额外设置朝向。
        /// </summary>
        public void MoveSteerTo(Vector3 worldHeading, bool wantsRun)
        {
            m_CommandExecutor.SetMoveSteer(worldHeading, wantsRun);
        }

        /// <summary>
        /// 停止移动。
        /// </summary>
        public void StopMove()
        {
            m_CommandExecutor.StopMove();
        }
    }
}
