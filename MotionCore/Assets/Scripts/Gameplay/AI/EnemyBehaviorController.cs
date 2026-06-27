using MotionCore.Gameplay.Character;
using MotionCore.Gameplay.Combat;
using MotionCore.Gameplay.Targeting;
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
        Hurtbox m_Hurtbox;
        Vector3 m_HomePosition;
        Transform m_Target;
        float m_NextAttackTime;

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

        /// <summary>
        /// 当前正面朝向。
        /// </summary>
        public Vector3 Forward => m_Character.FacingRoot.forward;

        /// <summary>
        /// 是否处于受击硬直，BT 应在此期间让位、不下发移动。
        /// </summary>
        public bool IsStaggered => m_Character.StateMachine.CurrentState.Type == CharacterStateType.Hit;

        /// <summary>
        /// 是否正在攻击，BT 据此等待攻击播完。
        /// </summary>
        public bool IsAttacking
        {
            get
            {
                CharacterStateType state = m_Character.StateMachine.CurrentState.Type;
                return state == CharacterStateType.BasicAttack || state == CharacterStateType.HeavyAttack;
            }
        }

        /// <summary>
        /// 攻击是否已过冷却。作为攻击分支的前置条件，冷却结束即可重新进攻。
        /// </summary>
        public bool IsAttackReady => Time.time >= m_NextAttackTime;

        void Awake()
        {
            m_CommandExecutor = GetComponentInChildren<ICharacterCommandExecutor>();
            m_Hurtbox = transform.root.GetComponentInChildren<Hurtbox>();
            m_HomePosition = m_Character.transform.position;

            // 攻击朝向对准当前目标，无目标则维持正面。
            m_CommandExecutor.SetAttackFacingResolver(ResolveAttackFacing);
        }

        void OnEnable()
        {
            m_Hurtbox.HitReceived += OnHitReceived;
        }

        void OnDisable()
        {
            m_Hurtbox.HitReceived -= OnHitReceived;

            m_CommandExecutor.StopMove();
            m_Target = null;
        }

        /// <summary>
        /// 被命中即仇恨攻击者：锁其锁定点
        /// </summary>
        void OnHitReceived(HitEvent hitEvent)
        {
            Transform attacker = hitEvent.Result.Attacker;

            LockOnTarget lockOn = attacker.root.GetComponentInChildren<LockOnTarget>();
            SetTarget(lockOn.LockPoint);
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
        public void MoveSteerTo(Vector3 worldHeading, bool wantsRun, TurnSpeed turnSpeed = TurnSpeed.Locomotion)
        {
            m_CommandExecutor.SetMoveSteer(worldHeading, wantsRun, turnSpeed);
        }

        /// <summary>
        /// 横移：朝向锁定 worldFaceDirection，沿 worldMoveDirection 移动（绕圈/横向走位）。
        /// </summary>
        public void MoveStrafe(Vector3 worldMoveDirection, Vector3 worldFaceDirection, bool wantsRun, TurnSpeed turnSpeed = TurnSpeed.General)
        {
            m_CommandExecutor.SetMoveStrafe(worldMoveDirection, worldFaceDirection, wantsRun, turnSpeed);
        }

        /// <summary>
        /// 停止移动。
        /// </summary>
        public void StopMove()
        {
            m_CommandExecutor.StopMove();
        }

        /// <summary>
        /// 原地转向对准指定世界方向（不移动）；turnSpeed 选择转身快慢，默认战斗瞄准速度。
        /// </summary>
        public void FaceTo(Vector3 worldDirection, TurnSpeed turnSpeed = TurnSpeed.Combat)
        {
            m_CommandExecutor.SetFacingDirection(worldDirection, turnSpeed);
        }

        /// <summary>
        /// 触发一次攻击，attack 为空则用角色默认普攻；返回是否成功进入攻击。
        /// </summary>
        public bool TryAttack(AttackDefinition attack)
        {
            return attack != null ? m_CommandExecutor.TryAttack(attack) : m_CommandExecutor.TryBasicAttack();
        }

        /// <summary>
        /// 开始攻击冷却，seconds 秒内 IsAttackReady 为假。
        /// </summary>
        public void StartAttackCooldown(float seconds)
        {
            m_NextAttackTime = Time.time + seconds;
        }

        /// <summary>
        /// 攻击朝向：有目标时对准目标，否则维持当前正面。
        /// </summary>
        Vector3 ResolveAttackFacing()
        {
            return HasTarget ? m_Target.position - Position : m_Character.FacingRoot.forward;
        }
    }
}
