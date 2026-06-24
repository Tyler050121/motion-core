using Animancer;
using Animancer.FSM;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class MoveState : CharacterState
    {
        /// <summary>
        /// 移动状态的内部阶段，取代多个互斥的布尔标志。
        /// </summary>
        enum MovePhase
        {
            Starting,    // 播放起步动画，尚未进入循环
            Looping,     // 移动循环中，可按走/跑切换动画
            Exiting,     // 播放收招动画，准备回 Idle
            TurningBack, // 播放急转身动画
        }

        ITimerService m_Timer;
        MotorConfig m_MotorConfig;
        LocomotionAnimationProfile m_LocomotionProfile;
        readonly TimerHandle m_RunTurnBackTimer = new();

        MovePhase m_Phase;

        public override CharacterStateType Type => CharacterStateType.Move;

        public void SetContext(
            ITimerService timer,
            MotorConfig motorConfig,
            LocomotionAnimationProfile locomotionProfile)
        {
            m_Timer = timer;
            m_MotorConfig = motorConfig;
            m_LocomotionProfile = locomotionProfile;
        }

        public override bool CanExitState
        {
            get
            {
                // 切向 Idle 前先补一段收招动画，收招结束才真正放行。
                CharacterState nextState = Character.StateMachine.NextState;
                if (m_Phase != MovePhase.Exiting && nextState.Type == CharacterStateType.Idle)
                    PlayMoveEnd();

                return base.CanExitState;
            }
        }

        void OnEnable()
        {
            ExitOptions = CharacterStateExitOptions.All;
            m_Phase = MovePhase.Starting;

            // 从 Idle 进入时先播起步，否则（如收招中途重新移动）直接进循环。
            bool fromIdle = StateChange<CharacterState>.PreviousState.Type == CharacterStateType.Idle;
            if (fromIdle && PlayStage(m_LocomotionProfile.MoveStart, PlayMoveLoop))
                return;

            PlayMoveLoop();
        }

        void OnDisable()
        {
            m_Timer.RemoveByOwner(this);
        }

        void Update()
        {
            SyncLocomotionBlend();
            TryResumeFromExit();

            if (ShouldPlayRunTurnBack())
            {
                PlayRunTurnBack();
                return;
            }

            if (m_Phase == MovePhase.TurningBack)
                return;

            // 移动转身用更慢的 locomotion 速度，让转身摊进走路循环，避免起步时原地急转。
            Character.Parameters.SetFacing(Character.Parameters.MoveDirection, m_MotorConfig.LocomotionTurnDuration);
        }

        /// <summary>
        /// 循环阶段按需在走/跑动画间切换，并每帧把混合参数同步到当前动画，避免速度/方向混合被冻结。
        /// </summary>
        void SyncLocomotionBlend()
        {
            // 急转身不属于走/跑混合，不参与参数同步。
            if (m_Phase == MovePhase.TurningBack)
                return;

            if (m_Phase == MovePhase.Looping)
                RefreshMoveLoopRoute();

            AnimancerState currentState = Character.Animancer.Layers[0].CurrentState;
            if (currentState != null)
                ApplyBlendParameter(currentState);
        }

        /// <summary>
        /// 收招过程中若重新输入移动，则取消收招回到循环。
        /// </summary>
        void TryResumeFromExit()
        {
            if (m_Phase == MovePhase.Exiting && Character.Parameters.HasMoveInput)
                PlayMoveLoop();
        }

        void PlayMoveLoop()
        {
            m_Phase = PlayStage(m_LocomotionProfile.MoveLoop, null)
                ? MovePhase.Looping
                : MovePhase.Starting;
        }

        void PlayMoveEnd()
        {
            m_Phase = MovePhase.Exiting;

            // 没有配置收招动画时直接放行回 Idle。
            if (!PlayStage(m_LocomotionProfile.MoveEnd, ReturnToDefaultState))
                ExitOptions |= CharacterStateExitOptions.Idle;
        }

        void PlayRunTurnBack()
        {
            m_Phase = MovePhase.TurningBack;
            Character.Parameters.ClearFacing();
            m_Timer.Delay(this, m_MotorConfig.RunTurnBackCooldown, m_RunTurnBackTimer);

            AnimancerState state = Character.Animancer.Play(m_LocomotionProfile.RunTurnBack);
            state.Events(this).OnEnd = PlayMoveLoop;
        }

        /// <summary>
        /// 判断是否应触发急转身：奔跑中且移动方向与朝向夹角超过阈值。
        /// </summary>
        bool ShouldPlayRunTurnBack()
        {
            if (m_Phase == MovePhase.TurningBack)
                return false;

            if (m_RunTurnBackTimer.IsActive)
                return false;

            if (!m_LocomotionProfile.RunTurnBack)
                return false;

            if (!Character.Parameters.IsRunning || !Character.Parameters.HasMoveInput)
                return false;

            Vector3 moveDirection = Character.Parameters.MoveDirection;
            moveDirection.y = 0f;

            Vector3 facingDirection = Character.FacingRoot.forward;
            facingDirection.y = 0f;

            return Vector3.Angle(facingDirection, moveDirection) >= m_MotorConfig.RunTurnBackAngle;
        }

        /// <summary>
        /// 播放指定阶段动画并记录为当前动画槽。
        /// 返回是否成功播放（阶段未配置动画时返回 false）。
        /// </summary>
        bool PlayStage(LocomotionAnimationStage stage, System.Action onEnd)
        {
            TransitionAsset transition = SelectTransition(stage);
            if (!transition)
                return false;

            AnimancerState state = Character.Animancer.Play(transition);
            ApplyBlendParameter(state);

            if (onEnd != null)
                state.Events(this).OnEnd = onEnd;

            return true;
        }

        /// <summary>
        /// 按当前走/跑状态重新选择循环动画，目标与当前一致时跳过，否则切换过去。
        /// </summary>
        void RefreshMoveLoopRoute()
        {
            TransitionAsset target = SelectTransition(m_LocomotionProfile.MoveLoop);

            AnimancerState currentState = Character.Animancer.Layers[0].CurrentState;
            if (currentState != null && target != null && currentState.Key == target.Key)
                return;

            PlayStage(m_LocomotionProfile.MoveLoop, null);
        }

        /// <summary>
        /// 根据阶段的路由模式与当前走/跑状态选择要播放的动画。
        /// </summary>
        TransitionAsset SelectTransition(LocomotionAnimationStage stage)
        {
            if (stage == null)
                return null;

            return stage.RouteMode switch
            {
                LocomotionStageRouteMode.Single => stage.SingleTransition,
                LocomotionStageRouteMode.SplitWalkRun => Character.Parameters.IsRunning
                    ? stage.RunTransition
                    : stage.WalkTransition,
                _ => null,
            };
        }

        /// <summary>
        /// 按当前播放状态的混合类型写入对应参数（速度 / 方向），非混合动画不处理。
        /// </summary>
        void ApplyBlendParameter(AnimancerState state)
        {
            switch (state)
            {
                case LinearMixerState linearMixer:
                    linearMixer.Parameter = Character.Parameters.MoveSpeed;
                    break;
                case Vector2MixerState directionMixer:
                    directionMixer.Parameter = Character.Parameters.MoveInput;
                    break;
            }
        }
    }
}
