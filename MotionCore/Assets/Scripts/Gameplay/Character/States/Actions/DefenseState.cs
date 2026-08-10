using Animancer;
using MotionCore.Gameplay.Combat;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 持续播放防御循环并降低承受伤害，输入松开后播放结束动画退出。
    /// </summary>
    public sealed class DefenseState : CharacterState
    {
        enum DefensePhase
        {
            Looping,
            Ending,
        }

        [SerializeField] TransitionAsset m_Loop;
        [SerializeField] TransitionAsset m_End;
        [SerializeField] MoveState m_MoveState;
        [SerializeField] Hurtbox m_Hurtbox;
        [SerializeField, Range(0f, 1f), Tooltip("防御承受伤害倍率，0.8 表示减伤 20%")]
        float m_DamageMultiplier = 0.8f;

        DefensePhase m_Phase;
        AnimancerState m_LoopAnimation;
        bool m_IsDefenseHeld;
        bool m_IsExitRequested;
        bool m_StartWithEnd;
        float m_LoopExitNormalizedTime;

        public override CharacterStateType Type => CharacterStateType.Defense;
        public override CastPriority CurrentCastPriority => CastPriority.Defense;
        public override StaggerLevel CurrentStaggerLevel
            => m_Phase == DefensePhase.Looping ? StaggerLevel.Block : StaggerLevel.None;

        public void SetDefenseHeld(bool isHeld)
        {
            m_IsDefenseHeld = isHeld;
        }

        /// <summary>
        /// 防御输入重新按下时继续循环；若仍在等待循环末尾，则取消本次退出。
        /// </summary>
        public void ResumeDefense()
        {
            m_IsExitRequested = false;

            if (m_Phase == DefensePhase.Ending)
                PlayLoop();
        }

        /// <summary>
        /// 从卸势进入统一的防御结束流程，避免两个状态同时拥有同一个 End 动画事件。
        /// </summary>
        public void EnterEnd()
        {
            m_StartWithEnd = true;
            Character.StateMachine.ForceSetState(this);
        }

        void OnEnable()
        {
            // 卸势松开后只复用防御结束动画，不进入防御循环。
            if (m_StartWithEnd)
            {
                m_StartWithEnd = false;
                PlayEnd();
                return;
            }

            PlayLoop();
        }

        void Update()
        {
            // 持续防御，或重新按下防御取消尚未完成的退出。
            if (m_IsDefenseHeld)
                m_IsExitRequested = false;
            // 松开防御且没有移动输入时不播放 End，直接回待机。
            else if (!Character.Parameters.HasMoveInput)
            {
                ReturnToDefaultState();
                return;
            }
            // 有移动输入时等待当前 Loop 播完，再用 End 过渡到移动。
            else if (m_Phase == DefensePhase.Looping)
            {
                if (!m_IsExitRequested)
                {
                    m_IsExitRequested = true;
                    m_LoopExitNormalizedTime = Mathf.Floor(m_LoopAnimation.NormalizedTime) + 1f;
                }

                if (m_LoopAnimation.NormalizedTime >= m_LoopExitNormalizedTime)
                    PlayEnd();
            }

            if (!Character.Parameters.HasMoveInput)
                return;

            Character.Parameters.SetFacing(
                Character.Parameters.MoveDirection,
                Character.Parameters.LocomotionTurnDuration);
        }

        void LateUpdate()
        {
            KeepRunSpeed();
        }

        void OnDisable()
        {
            m_Hurtbox.SetDamageMultiplier(1f);
        }

        /// <summary>
        /// 防御循环和结束动画保持跑步速度，进入 Move 后再由移动输入平滑降速。
        /// </summary>
        void KeepRunSpeed()
        {
            CharacterParameters parameters = Character.Parameters;
            parameters.SetMove(
                parameters.MoveInput,
                parameters.MoveDirection,
                GlobalConfig.Locomotion.RunSpeed,
                true,
                parameters.LocomotionTurnDuration);
        }

        void PlayLoop()
        {
            m_Phase = DefensePhase.Looping;
            m_IsExitRequested = false;
            m_Hurtbox.SetDamageMultiplier(m_DamageMultiplier);

            // TODO: 防御通常应使用原地 Loop；若允许移动防御，应补齐方向和速度 Mixer。
            // 当前暂用资源库仅有的带位移 Loop 动画。
            m_LoopAnimation = PlayAnimation(m_Loop);
            ExitWindows = CharacterExitWindow.Attack;
        }

        void PlayEnd()
        {
            m_Phase = DefensePhase.Ending;
            m_Hurtbox.SetDamageMultiplier(1f);
            PlayWithEvents(m_End, FinishEnd);
            ExitWindows = CharacterExitWindow.Attack;
        }

        void FinishEnd()
        {
            if (Character.Parameters.HasMoveInput)
                Character.StateMachine.ForceSetState(m_MoveState);
            else
                ReturnToDefaultState();
        }
    }
}
