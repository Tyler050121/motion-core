using Animancer;
using Animancer.FSM;
using UnityEngine;
using EventNames = MotionCore.GlobalConfig.AnimationEventNames;

namespace MotionCore.Gameplay.Character
{
    public abstract class CharacterState : StateBehaviour
    {
        [SerializeField] Character m_Character;

        AnimancerState m_CurrentAnimation;
        bool m_CanInterruptSelf;

        protected Character Character => m_Character;
        protected CharacterExitWindow ExitWindows { get; set; } = CharacterExitWindow.None;

        /// <summary>
        /// 同状态能否重入。默认由动画上的 CanInterrupt 事件开启，每次播放重置。
        /// 本身就有对应能力位的状态（如闪避、攻击）直接重写成读那一位即可。
        /// </summary>
        protected virtual bool CanInterruptSelf => m_CanInterruptSelf;

        /// <summary>
        /// 正在派发的动画事件是否属于当前动画。
        /// 动画事件排到帧末批量派发，同一批里前面的回调可能已经切走了动画甚至退出了本状态，
        /// 属于旧动画的事件必须丢弃。
        /// </summary>
        bool IsEventFromCurrentAnimation
            => enabled
            && m_CurrentAnimation != null
            && AnimancerEvent.Invocation.Current.State == m_CurrentAnimation;

        /// <summary>
        /// 当前动作的施法优先级。
        /// </summary>
        public abstract CastPriority CurrentCastPriority { get; }

        /// <summary>
        /// 下一次请求动作的施法优先级。普通状态与当前优先级一致，复用状态实例的动作可重写为待执行动作优先级。
        /// </summary>
        public virtual CastPriority RequestedCastPriority => CurrentCastPriority;

        /// <summary>
        /// 当前人物状态等级。命中僵直等级达到或超过该值时进入受击反应。
        /// </summary>
        public abstract StaggerLevel CurrentStaggerLevel { get; }

        public abstract CharacterStateType Type { get; }

        protected void OpenCancel() => ExitWindows |= CharacterExitWindow.Move | CharacterExitWindow.Attack;
        protected void OpenEvade() => ExitWindows |= CharacterExitWindow.Evade;
        protected void OpenAttack() => ExitWindows |= CharacterExitWindow.Attack;
        void OpenInterruptSelf() => m_CanInterruptSelf = true;

        protected void ReturnToDefaultState() => Character.StateMachine.ForceSetDefaultState();

        /// <summary>
        /// 播放动画、绑定动画事件，并关闭主动退出窗口等待显式 CanX 事件开放。
        /// 事件只在首次播放该动画时绑定一次、之后重播复用，所以回调里不要捕获随播放变化的局部变量，需要的话存成字段。
        /// </summary>
        protected void PlayWithEvents(ITransition transition, System.Action onEnd)
        {
            m_CurrentAnimation = Character.Animancer.Play(transition);

            if (m_CurrentAnimation.Events(this, out AnimancerEvent.Sequence events))
            {
                for (int i = 0; i < events.Count; i++)
                    BindEvent(events, i, events.GetName(i));
            }

            m_CanInterruptSelf = false;
            ExitWindows = CharacterExitWindow.None;

            // 结束事件同样要过滤：动画被打断后仍在淡出时，Animancer 每帧都会重发它。
            events.OnEnd = () =>
            {
                if (IsEventFromCurrentAnimation)
                    onEnd();
            };
        }

        /// <summary>
        /// 绑定单个动画事件。基类处理各状态通用的能力窗口事件，子类重写后把未命中的交回 base。
        /// 一律用 Bind 绑定，不要直接 SetCallback，否则拿不到旧动画事件的过滤。
        /// </summary>
        protected virtual void BindEvent(AnimancerEvent.Sequence events, int index, string name)
        {
            if (name == EventNames.CanCancel)
                Bind(events, index, OpenCancel);
            else if (name == EventNames.CanEvade)
                Bind(events, index, OpenEvade);
            else if (name == EventNames.CanAttack)
                Bind(events, index, OpenAttack);
            else if (name == EventNames.CanInterrupt)
                Bind(events, index, OpenInterruptSelf);
        }

        /// <summary>
        /// 绑定回调，并过滤掉不属于当前动画的事件。
        /// 包装只在首次绑定时做一次，回调本身不用再判断。
        /// </summary>
        protected void Bind(AnimancerEvent.Sequence events, int index, System.Action callback)
        {
            events.SetCallback(index, () =>
            {
                if (IsEventFromCurrentAnimation)
                    callback();
            });
        }

        protected void Bind(AnimancerEvent.Sequence events, int index, System.Action<int> callback)
        {
            events.AddCallback<int>(index, parameter =>
            {
                if (IsEventFromCurrentAnimation)
                    callback(parameter);
            });
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            gameObject.GetComponentInParentOrChildren(ref m_Character);
        }
#endif

        public override bool CanExitState
        {
            get
            {
                CharacterState nextState = m_Character.StateMachine.NextState;
                if (nextState == this)
                    return RequestedCastPriority > CurrentCastPriority || CanInterruptSelf;

                return CharacterStateRules.CanExit(
                    Type,
                    nextState.Type,
                    CurrentCastPriority,
                    nextState.RequestedCastPriority,
                    ExitWindows);
            }
        }
    }
}
