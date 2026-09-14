using Animancer;
using MotionCore.Gameplay.Combat;
using MotionCore.Infrastructure;
using UnityEngine;
using EventNames = MotionCore.GlobalConfig.AnimationEventNames;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 播放卸势动作，并仅在动画事件标记的窗口内拒绝来袭。
    /// </summary>
    public sealed class ParryState : CharacterState, IEventListener<HitParriedEvent>
    {
        [SerializeField] TransitionAsset m_Parry;
        [SerializeField] DefenseState m_DefenseState;
        [SerializeField] Hurtbox m_Hurtbox;
        [SerializeField, Min(0f), Tooltip("卸势成功对攻击者造成的架势伤害")]
        float m_PostureDamage = 25f;

        IEventBus m_EventBus;
        bool m_IsDefenseHeld;

        public override CharacterStateType Type => CharacterStateType.Parry;
        public override CastPriority CurrentCastPriority => CastPriority.Parry;
        public override StaggerLevel CurrentStaggerLevel => StaggerLevel.None;

        public void SetDefenseHeld(bool isHeld)
        {
            m_IsDefenseHeld = isHeld;
        }

        void Awake()
        {
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
        }

        void OnEnable()
        {
            m_EventBus.Subscribe<HitParriedEvent>(m_Hurtbox, this);

            // TODO: 卸势通常应使用原地动画；若允许移动，应补齐按移动速度混合的 Mixer 版本。
            // 当前暂用资源库仅有的带位移动画。
            PlayWithEvents(m_Parry, FinishParry);
        }

        void Update()
        {
            // 松开且无移动时立即退出
            if (!m_IsDefenseHeld && !Character.Parameters.HasMoveInput)
            {
                ReturnToDefaultState();
                return;
            }

            if (!Character.Parameters.HasMoveInput)
                return;

            // 有移动输入时持续转向
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
            m_EventBus.Unsubscribe<HitParriedEvent>(m_Hurtbox, this);
            CloseParry();
        }

        protected override void BindEvent(AnimancerEvent.Sequence events, int index, string name)
        {
            if (name == EventNames.ParryStart)
                Bind(events, index, OpenParry);
            else if (name == EventNames.ParryEnd)
                Bind(events, index, CloseParry);
            else
                base.BindEvent(events, index, name);
        }

        /// <summary>
        /// 每次卸势只响应第一次被拒绝的命中。
        /// </summary>
        public void OnEvent(HitParriedEvent eventData)
        {
            m_EventBus.Unsubscribe<HitParriedEvent>(m_Hurtbox, this);
            CloseParry();

            eventData.Attacker.GetComponentInParent<Posture>().ApplyDamage(m_PostureDamage);
        }

        void OpenParry() => m_Hurtbox.SetParrying(true);
        void CloseParry() => m_Hurtbox.SetParrying(false);

        /// <summary>
        /// 卸势期间保持跑步速度，退出后再由防御出口或移动状态接管。
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

        void FinishParry()
        {
            // 按住：进入 Defense Loop
            if (m_IsDefenseHeld)
            {
                Character.StateMachine.ForceSetState(m_DefenseState);
                return;
            }

            // 松开但有移动：进入 Defense End
            // 松开且无移动：立即回 Idle
            if (Character.Parameters.HasMoveInput)
                m_DefenseState.EnterEnd();
            else
                ReturnToDefaultState();
        }
    }
}
