#if GRAPH_DESIGNER
using MotionCore.Gameplay.Combat;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.Shared.Utility;
using UnityEngine;

namespace MotionCore.Gameplay.AI.BehaviorDesigner.Tasks.Actions
{
    /// <summary>
    /// 触发一次攻击并等待其播完：期间 Running，结束 Success 并开始冷却；无目标或触发失败返回 Failure。
    /// 冷却由前置条件 AttackReady 把守（本节点出手后写入 Controller 冷却），冷却期间攻击分支失败、同级分支（如绕走位）接管。
    /// 攻击类型由 Attack 字段指定，留空则用默认普攻；攻击朝向由 EnemyBehaviorController 设为对准目标。
    /// </summary>
    [Description("触发一次攻击并等待播完，出手后开始冷却")]
    public sealed class Attack : EnemyBehaviorAction
    {
        [Tooltip("要使用的攻击定义，留空则用角色默认普攻")]
        [SerializeField] AttackDefinition m_Attack;

        [Tooltip("出手后的攻击间隔，期间 AttackReady 为假")]
        [SerializeField] float m_Cooldown = 2f;

        bool m_Started;

        public override void OnStart()
        {
            m_Started = false;
        }

        public override TaskStatus OnUpdate()
        {
            if (!Controller.HasTarget)
                return TaskStatus.Failure;

            // 已发起：等待攻击播完，结束后开始冷却。
            if (m_Started)
            {
                if (Controller.IsAttacking)
                    return TaskStatus.Running;

                Controller.StartAttackCooldown(m_Cooldown);
                return TaskStatus.Success;
            }

            if (!Controller.TryAttack(m_Attack))
                return TaskStatus.Failure;

            m_Started = true;
            return TaskStatus.Running;
        }
    }
}
#endif
