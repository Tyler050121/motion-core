using System;
using Animancer.FSM;
using Animancer.Units;
using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Combat;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 角色命令控制器。
    /// 负责接收外部控制指令（玩家输入或AI逻辑），管理输入缓冲，并驱动角色的状态机（移动、闪避、攻击、受击等）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterCommandController : MonoBehaviour, ICharacterCommandExecutor, IConfigReceiver<CharacterDefinition>, IHitReactionHandler
    {
        [SerializeField] Character m_Character;
        [SerializeField] MoveState m_MoveState;
        [SerializeField] EvadeState m_EvadeState;
        [SerializeField] AttackState m_AttackState;
        [SerializeField] HitState m_HitState;
        
        [Tooltip("指令输入的缓冲时间")]
        [SerializeField, Seconds] float m_InputTimeOut = 0.35f;

        // 状态机输入缓冲区，管理带有效期的指令队列
        StateMachine<CharacterState>.InputBuffer m_InputBuffer;
        MotorConfig m_MotorConfig;
        AttackDefinition m_BasicAttack;
        Func<Vector3> m_AttackFacingResolver;

        public CharacterStateType CurrentStateType => m_Character.StateMachine.CurrentState.Type;
        public float MoveSpeed => m_Character.Parameters.MoveSpeed;

        void Awake()
        {
            // 初始化默认的攻击朝向解析器，并将解析器传递给攻击状态
            m_AttackFacingResolver = GetDefaultAttackFacingDirection;
            m_AttackState.SetAttackFacingResolver(GetAttackFacingDirection);
            
            // 实例化输入缓冲区绑定角色的动作状态机
            m_InputBuffer = new StateMachine<CharacterState>.InputBuffer(m_Character.StateMachine);
        }

        public void Initialize(CharacterDefinition definition)
        {
            m_MotorConfig = definition.Motor;
            m_BasicAttack = definition.BasicAttack;

            ITimerService timer = ServiceLocator.Resolve<ITimerService>();
            m_MoveState.SetContext(timer, m_MotorConfig, definition.LocomotionAnimation);
        }

        void Update()
        {
            // 每帧更新输入缓冲区，尝试消费缓冲队列中尚未过期的指令状态
            m_InputBuffer.Update();
        }

        void LateUpdate()
        {
            if (!m_Character.Parameters.HasFacingDirection)
                return;

            // 在 LateUpdate 中统一处理朝向旋转，确保所有动作逻辑计算完毕后平滑转身
            TurnFacingToward(m_Character.Parameters.FacingDirection);
        }

        public void SetAttackFacingResolver(Func<Vector3> resolver)
        {
            m_AttackFacingResolver = resolver;
        }

        /// <summary>
        /// 接收并处理移动输入，包含速度插值以及移动/待机状态和黑板参数的切换。
        /// </summary>
        public void SetMoveInput(Vector2 moveInput, Vector3 moveDirection, bool wantsRun)
        {
            bool hasMoveInput = moveDirection.sqrMagnitude > 0.0001f;
            // 限制输入向量的长度避免对角线移动比直线快
            Vector2 clampedInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            Vector3 planarDirection = NormalizePlanar(moveDirection);
            
            // 根据是否有输入以及是否想要奔跑，确定目标速度
            float targetSpeed = hasMoveInput ? (wantsRun ? m_MotorConfig.RunSpeed : m_MotorConfig.WalkSpeed) : 0f;
            
            // 根据当前目标速度判断采用何种加速度/减速度（WalkSpeedChangeRate 或 RunSpeedChangeRate）
            float speedChangeRate = targetSpeed <= m_MotorConfig.WalkSpeed ? m_MotorConfig.WalkSpeedChangeRate : m_MotorConfig.RunSpeedChangeRate;
            
            // 平滑计算当前帧的移动速度
            float moveSpeed = Mathf.MoveTowards(m_Character.Parameters.MoveSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

            // 更新角色的共享参数（黑板），以供各动作状态查询
            m_Character.Parameters.SetMove(clampedInput, planarDirection, moveSpeed, moveSpeed > m_MotorConfig.RunThresholdSpeed);

            // 如果有移动输入则尝试进入移动状态，否则切回到默认状态（通常是Idle）
            if (hasMoveInput)
                m_Character.StateMachine.TrySetState(m_MoveState);
            else
                m_Character.StateMachine.TrySetDefaultState();
        }

        /// <summary>
        /// 朝向目标方向前进（orient-to-move）：混合动画固定播本地前进，身体转向 worldHeading，
        /// 配合 root motion 沿当前朝向位移，从而边走边转、走出弧线。适合巡逻/追击这种"面朝去向"的移动。
        /// </summary>
        public void SetMoveSteer(Vector3 worldHeading, bool wantsRun)
        {
            Vector3 planarHeading = NormalizePlanar(worldHeading);
            if (planarHeading.sqrMagnitude <= 0.0001f)
            {
                StopMove();
                return;
            }

            // MoveInput 固定为本地前进 (0,1) → 始终播前进走路；
            // moveDirection 传 heading → MoveState 把身体朝向转向它，实现边走边转。
            SetMoveInput(new Vector2(0f, 1f), planarHeading, wantsRun);
        }

        public void StopMove()
        {
            SetMoveInput(Vector2.zero, Vector3.zero, false);
        }

        public void SetFacingDirection(Vector3 facingDirection)
        {
            m_Character.Parameters.SetFacing(facingDirection);
        }

        /// <summary>
        /// 尝试执行闪避动作。通过 InputBuffer 提高判定宽松度。
        /// </summary>
        public bool TryEvade()
        {
            m_InputBuffer.Buffer(m_EvadeState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }

        public bool TryBasicAttack()
        {
            return TryAttack(m_BasicAttack);
        }

        /// <summary>
        /// 尝试执行任意攻击动作。处理连招排列与动作排队逻辑。
        /// </summary>
        public bool TryAttack(AttackDefinition definition)
        {
            // 如果连招验证不通过则不允许入队
            if (!m_AttackState.QueueAttack(definition))
                return false;

            // 若当前已经是攻击状态，则成功挂起（等待连招进入下一段）
            if (m_Character.StateMachine.CurrentState == m_AttackState)
                return true;

            // 缓冲攻击状态尝试立刻进入
            m_InputBuffer.Buffer(m_AttackState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }

        /// <summary>
        /// 接收受击指令并强制切断当前动作，进入受击硬直状态。
        /// </summary>
        public void ReceiveHit(float knockbackPower)
        {
            m_HitState.SetContext(knockbackPower);
            // 如果已经是受击状态则重置该状态重新开始，否则直接进入
            if (m_Character.StateMachine.CurrentState == m_HitState)
                m_Character.StateMachine.TryResetState(m_HitState);
            else
                m_Character.StateMachine.TrySetState(m_HitState);
        }

        Vector3 GetAttackFacingDirection()
        {
            return NormalizePlanar(m_AttackFacingResolver());
        }

        /// <summary>
        /// 获取默认的攻击朝向：如果有移动输入则面向摇杆方向，否则维持当前正面朝向。
        /// </summary>
        Vector3 GetDefaultAttackFacingDirection()
        {
            if (m_Character.Parameters.HasMoveInput)
                return m_Character.Parameters.MoveDirection;

            return m_Character.FacingRoot.forward;
        }

        /// <summary>
        /// 提取方向在XZ平面上的投影，并进行归一化。
        /// </summary>
        Vector3 NormalizePlanar(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }

        /// <summary>
        /// 旋转角色的朝向节点使其面向指定方向。
        /// </summary>
        /// <param name="facingDirection">目标朝向的世界空间方向向量。</param>
        void TurnFacingToward(Vector3 facingDirection)
        {
            // 忽略垂直方向（Y轴），以确保仅在水平面进行旋转
            facingDirection.y = 0f;
            Transform facingRoot = m_Character.FacingRoot;
            
            // 获取目标方向相对世界向上方向的旋转四元数
            Quaternion targetRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);

            // 优先用本次设置的转身时长（如移动用更慢的 locomotion 速度），未指定则回落到配置默认
            float turnDuration = m_Character.Parameters.FacingTurnDuration >= 0f
                ? m_Character.Parameters.FacingTurnDuration
                : m_MotorConfig.FacingTurnDuration;

            // 如果旋转持续时间小等于0，则瞬间完成转向并清除朝向标志
            if (turnDuration <= 0f)
            {
                facingRoot.rotation = targetRotation;
                m_Character.Parameters.ClearFacing();
                return;
            }

            // 根据转身时间，计算按帧率应该旋转的最大度数（每半圈180度的时间分配）
            float maxDegreesDelta = 180f / turnDuration * Time.deltaTime;
            // 平滑旋转至目标角度
            facingRoot.rotation = Quaternion.RotateTowards(facingRoot.rotation, targetRotation, maxDegreesDelta);

            // 检查当前旋转度与目标旋转度的误差，如果小于0.1度则认为转身已完成
            if (Quaternion.Angle(facingRoot.rotation, targetRotation) <= 0.1f)
                m_Character.Parameters.ClearFacing();
        }
    }
}
