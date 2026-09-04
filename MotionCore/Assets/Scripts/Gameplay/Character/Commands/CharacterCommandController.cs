using System;
using MotionCore;
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
    public sealed class CharacterCommandController : MonoBehaviour,
        ICharacterCommandExecutor,
        IConfigReceiver<CharacterDefinition>,
        IHitReactionHandler,
        IEventListener<PostureChangedEvent>,
        IEventListener<HealthChangedEvent>
    {
        [SerializeField] Character m_Character;
        [SerializeField] MoveState m_MoveState;
        [SerializeField] EvadeState m_EvadeState;
        [SerializeField] ParryState m_ParryState;
        [SerializeField] DefenseState m_DefenseState;
        [SerializeField] AttackState m_AttackState;
        [SerializeField] HitState m_HitState;
        [SerializeField] PostureBreakState m_PostureBreakState;
        [SerializeField] ExecutionState m_ExecutionState;
        [SerializeField] DeadState m_DeadState;
        
        [Tooltip("指令输入的缓冲时间")]
        [SerializeField, Seconds] float m_InputTimeOut = 0.35f;

        // 状态机输入缓冲区，管理带有效期的指令队列
        StateMachine<CharacterState>.InputBuffer m_InputBuffer;
        MotorConfig m_MotorConfig;
        AttackDefinition m_BasicAttack;
        AttackDefinition m_DodgeCounterAttack;
        Func<Vector3> m_AttackFacingResolver;
        Posture m_Posture;
        Health m_Health;
        IEventBus m_EventBus;
        CharacterState m_PendingReactionState;

        public CharacterStateType CurrentStateType => m_Character.StateMachine.CurrentState.Type;
        public float MoveSpeed => m_Character.Parameters.MoveSpeed;

        void Awake()
        {
            // 初始化默认的攻击朝向解析器，并将解析器传递给攻击状态
            m_AttackFacingResolver ??= GetDefaultAttackFacingDirection;
            m_AttackState.SetAttackFacingResolver(GetAttackFacingDirection);
            
            // 实例化输入缓冲区绑定角色的动作状态机
            m_InputBuffer = new StateMachine<CharacterState>.InputBuffer(m_Character.StateMachine);
            m_Posture = m_Character.GetComponent<Posture>();
            m_Health = m_Character.GetComponent<Health>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
        }

        void OnEnable()
        {
            m_EventBus.Subscribe<PostureChangedEvent>(m_Posture, this);
            m_EventBus.Subscribe<HealthChangedEvent>(m_Health, this);
        }

        void OnDisable()
        {
            m_EventBus.Unsubscribe<PostureChangedEvent>(m_Posture, this);
            m_EventBus.Unsubscribe<HealthChangedEvent>(m_Health, this);
            m_PendingReactionState = null;
        }

        public void Initialize(CharacterDefinition definition)
        {
            m_MotorConfig = definition.Motor;
            m_BasicAttack = definition.BasicAttack;
            m_DodgeCounterAttack = definition.DodgeCounterAttack;

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
            CharacterState pendingState = m_PendingReactionState;
            m_PendingReactionState = null;
            if (pendingState != null)
                m_Character.StateMachine.ForceSetState(pendingState);

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
        public void SetMoveInput(Vector2 moveInput, Vector3 moveDirection, bool wantsRun, float turnDuration = -1f)
        {
            bool hasMoveInput = moveDirection.sqrMagnitude > 0.0001f;
            // 限制输入向量的长度避免对角线移动比直线快
            Vector2 clampedInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            Vector3 planarDirection = NormalizePlanar(moveDirection);
            
            // 根据是否有输入以及是否想要奔跑，确定目标速度
            float targetSpeed = hasMoveInput ? (wantsRun ? GlobalConfig.Locomotion.RunSpeed : GlobalConfig.Locomotion.WalkSpeed) : 0f;

            // 跑步降到步行单独放缓，避免卸势过渡进入移动后速度骤降。
            bool isSlowingToWalk = targetSpeed == GlobalConfig.Locomotion.WalkSpeed
                && m_Character.Parameters.MoveSpeed > GlobalConfig.Locomotion.WalkSpeed;
            float speedChangeRate = isSlowingToWalk
                ? GlobalConfig.Locomotion.RunToWalkSpeedChangeRate
                : targetSpeed <= GlobalConfig.Locomotion.WalkSpeed
                    ? GlobalConfig.Locomotion.WalkSpeedChangeRate
                    : GlobalConfig.Locomotion.RunSpeedChangeRate;
            
            // 平滑计算当前帧的移动速度
            float moveSpeed = Mathf.MoveTowards(m_Character.Parameters.MoveSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

            // IsRunning 取「想跑」意图，起步即按跑步选起步动画；松开后仍在跑速以上则保持跑步，平滑过渡回走。
            bool isRunning = wantsRun || moveSpeed > GlobalConfig.Locomotion.RunThresholdSpeed;

            // 更新角色的共享参数（黑板），以供各动作状态查询
            m_Character.Parameters.SetMove(clampedInput, planarDirection, moveSpeed, isRunning, turnDuration);

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
        public void SetMoveSteer(Vector3 worldHeading, bool wantsRun, TurnSpeed turnSpeed = TurnSpeed.Locomotion)
        {
            Vector3 planarHeading = NormalizePlanar(worldHeading);
            if (planarHeading.sqrMagnitude <= 0.0001f)
            {
                StopMove();
                return;
            }

            float turnDuration = ResolveTurnDuration(turnSpeed);

            // MoveInput 固定为本地前进 (0,1) → 始终播前进走路；
            // moveDirection 传 heading → MoveState 把身体朝向转向它，实现边走边转。
            SetMoveInput(new Vector2(0f, 1f), planarHeading, wantsRun, turnDuration);
        }

        /// <summary>
        /// 横移：身体朝向锁定 worldFaceDirection（如对准玩家），沿 worldMoveDirection 移动，
        /// 移动方向转到本地空间喂方向混合播出侧/后移，从而实现绕圈、横向走位。
        /// </summary>
        public void SetMoveStrafe(Vector3 worldMoveDirection, Vector3 worldFaceDirection, bool wantsRun, TurnSpeed turnSpeed = TurnSpeed.General)
        {
            Vector3 planarFace = NormalizePlanar(worldFaceDirection);
            if (planarFace.sqrMagnitude <= 0.0001f)
            {
                StopMove();
                return;
            }

            Vector3 localMove = m_Character.FacingRoot.InverseTransformDirection(NormalizePlanar(worldMoveDirection));
            localMove.y = 0f;
            Vector2 moveInput = new Vector2(localMove.x, localMove.z);

            float turnDuration = ResolveTurnDuration(turnSpeed);

            // moveDirection 传 facing → MoveState 把身体转向目标；moveInput 是本地横移方向。
            SetMoveInput(moveInput, planarFace, wantsRun, turnDuration);
        }

        public void StopMove()
        {
            SetMoveInput(Vector2.zero, Vector3.zero, false);
        }

        public void SetFacingDirection(Vector3 facingDirection, TurnSpeed turnSpeed = TurnSpeed.General)
        {
            m_Character.Parameters.SetFacing(facingDirection, ResolveTurnDuration(turnSpeed));
        }

        public void SetDefenseHeld(bool isHeld)
        {
            m_ParryState.SetDefenseHeld(isHeld);
            m_DefenseState.SetDefenseHeld(isHeld);
        }

        /// <summary>
        /// 把转身速度档位映射到 MotorConfig 上对应的 180 度转身时长。
        /// </summary>
        float ResolveTurnDuration(TurnSpeed turnSpeed)
        {
            return turnSpeed switch
            {
                TurnSpeed.Locomotion => m_MotorConfig.LocomotionTurnDuration,
                TurnSpeed.Combat => m_MotorConfig.CombatTurnDuration,
                _ => m_MotorConfig.FacingTurnDuration,
            };
        }

        /// <summary>
        /// 尝试执行闪避动作。通过 InputBuffer 提高判定宽松度。
        /// </summary>
        public bool TryEvade()
        {
            Vector3 evadeFacing = m_Character.Parameters.HasMoveInput
                ? m_Character.Parameters.MoveDirection
                : m_Character.FacingRoot.forward;

            evadeFacing.y = 0f;
            m_EvadeState.SetContext(evadeFacing, m_MotorConfig.EvadeTurnDuration);

            m_InputBuffer.Buffer(m_EvadeState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }

        public bool TryParry()
        {
            m_InputBuffer.Buffer(m_ParryState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }

        public bool TryDefense()
        {
            if (m_Character.StateMachine.CurrentState == m_DefenseState)
            {
                m_DefenseState.ResumeDefense();
                return true;
            }

            return m_Character.StateMachine.TrySetState(m_DefenseState);
        }

        public bool RefreshExecutionTarget()
        {
            bool isUnavailableForExecution = m_Health.IsDead
                || CurrentStateType == CharacterStateType.Execution
                || CurrentStateType == CharacterStateType.Executed;
            if (isUnavailableForExecution)
                return false;

            return m_ExecutionState.TryPrepare();
        }

        public bool TryExecution()
        {
            return m_Character.StateMachine.TrySetState(m_ExecutionState);
        }

        public bool TryBasicAttack()
        {
            bool useDodgeCounter = m_Character.StateMachine.CurrentState == m_EvadeState
                && m_EvadeState.TryConsumeDodgeCounter();
            AttackDefinition attack = useDodgeCounter
                ? m_DodgeCounterAttack
                : m_BasicAttack;
            return TryAttack(attack);
        }

        /// <summary>
        /// 尝试执行任意攻击动作。处理连招排列与动作排队逻辑。
        /// </summary>
        public bool TryAttack(AttackDefinition definition)
        {
            m_AttackState.QueueAttack(definition);

            // 同一个 AttackState 复用多种动作：高优先级请求立即重入，其他请求等待 CanAttack/CanCancel。
            if (m_Character.StateMachine.CurrentState == m_AttackState)
            {
                m_Character.StateMachine.TryResetState(m_AttackState);
                return true;
            }

            // 缓冲攻击状态尝试立刻进入
            m_InputBuffer.Buffer(m_AttackState, m_InputTimeOut);
            return m_InputBuffer.Update(0f);
        }

        /// <summary>
        /// 接收受击指令。
        /// </summary>
        public void ReceiveHit(StaggerLevel staggerLevel, float knockbackPower)
        {
            CharacterState currentState = m_Character.StateMachine.CurrentState;

            if (currentState == m_PostureBreakState)
            {
                // 破韧期间保持当前状态，只替换受击表现。
                m_PostureBreakState.PlayBreakHit();
                return;
            }

            StaggerLevel currentStaggerLevel = currentState.CurrentStaggerLevel;
            // 只有技能僵直等级达到当前人物状态等级时才进入受击硬直状态。
            if (staggerLevel == StaggerLevel.None || staggerLevel < currentStaggerLevel)
                return;

            m_HitState.SetContext(knockbackPower);
            // 如果已经是受击状态则重置该状态重新开始，否则直接进入
            if (m_Character.StateMachine.CurrentState == m_HitState)
                m_Character.StateMachine.TryResetState(m_HitState);
            else
                m_Character.StateMachine.TrySetState(m_HitState);
        }

        /// <summary>
        /// 处理架势破防事件，并只在刚进入破防时开启破防状态或处决窗口。
        /// </summary>
        public void OnEvent(PostureChangedEvent eventData)
        {
            if (!eventData.IsNewlyBroken)
                return;

            if (m_PostureBreakState.LocksCharacter)
                m_PendingReactionState = m_PostureBreakState;
            else
                m_PostureBreakState.OpenExecutionWindow();
        }

        public void OnEvent(HealthChangedEvent eventData)
        {
            if (eventData.Source.IsDead)
                m_PendingReactionState = m_DeadState;
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
