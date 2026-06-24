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

        ICharacterCommandExecutor m_CommandExecutor;
        Vector3 m_HomePosition;
        Transform m_Target;

        /// <summary>
        /// 敌人当前世界坐标。
        /// </summary>
        public Vector3 Position => m_Character.transform.position;

        /// <summary>
        /// 出生点，用作巡逻锚点。
        /// </summary>
        public Vector3 HomePosition => m_HomePosition;

        /// <summary>
        /// 当前索敌得到的目标（如玩家的锁定点）；未锁定时为 null。
        /// </summary>
        public Transform Target => m_Target;

        /// <summary>
        /// 是否已有有效目标。
        /// </summary>
        public bool HasTarget => m_Target != null;

        void Awake()
        {
            m_CommandExecutor = GetComponentInChildren<ICharacterCommandExecutor>();
            m_HomePosition = m_Character.transform.position;
        }

        void OnDisable()
        {
            m_CommandExecutor.StopMove();
            m_Target = null;
        }

        /// <summary>
        /// 记录当前索敌目标。
        /// </summary>
        public void SetTarget(Transform target)
        {
            m_Target = target;
        }

        /// <summary>
        /// 清除当前目标。
        /// </summary>
        public void ClearTarget()
        {
            m_Target = null;
        }

        /// <summary>
        /// 朝目标方向前进，身体边走边转向目标（走出弧线）。turnSpeed 选择转身快慢。
        /// </summary>
        public void MoveSteerTo(Vector3 worldHeading, bool wantsRun, LocomotionTurnSpeed turnSpeed = LocomotionTurnSpeed.Locomotion)
        {
            m_CommandExecutor.SetMoveSteer(worldHeading, wantsRun, turnSpeed);
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
