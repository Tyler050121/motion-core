using Animancer;
using MotionCore.Gameplay.Combat;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 播放破韧起始、等待与恢复流程。
    /// </summary>
    public sealed class PostureBreakState : CharacterState
    {
        enum PostureBreakPhase
        {
            Starting,
            Waiting,
            Ending,
        }

        [SerializeField, Tooltip("破韧起始动画")] TransitionAsset m_Start;
        [SerializeField, Tooltip("破韧等待动画")] TransitionAsset m_Loop;
        [SerializeField, Tooltip("破韧期间受击动画")] TransitionAsset m_BreakHit;
        [SerializeField, Tooltip("破韧恢复动画")] TransitionAsset m_End;
        [SerializeField, Min(0f), Tooltip("Waiting 阶段的可处决窗口持续时间")]
        float m_ExecutionWindow = 5f;
        [SerializeField, Tooltip("Waiting 阶段是否锁定角色并播放等待动画")]
        bool m_LockDuringWaiting = true;
        [SerializeField] ExecutedState m_ExecutedState;

        PostureBreakPhase m_Phase;
        Posture m_Posture;

        public override CharacterStateType Type => CharacterStateType.PostureBreak;
        public override CastPriority CurrentCastPriority => CastPriority.None;
        public override StaggerLevel CurrentStaggerLevel => StaggerLevel.PostureBreak;
        public bool LocksCharacter => m_LockDuringWaiting;

        void Awake()
        {
            m_Posture = Character.GetComponent<Posture>();
        }

        void OnEnable()
        {
            m_Phase = PostureBreakPhase.Starting;
            OpenExecutionWindow();
            PlayWithEvents(m_Start, PlayLoop);
        }

        public void OpenExecutionWindow()
        {
            m_ExecutedState.BeginWindow(m_ExecutionWindow, ExpireExecutionWindow);
        }

        /// <summary>
        /// 破韧期间受击时只切换表现动画，不离开破韧状态。
        /// </summary>
        public void PlayBreakHit()
        {
            if (m_BreakHit == null || m_Phase == PostureBreakPhase.Ending)
                return;

            m_Phase = PostureBreakPhase.Waiting;
            PlayWithEvents(m_BreakHit, PlayLoop);
            RestartAnimation();
        }

        /// <summary>
        /// 结束可处决窗口；锁定型目标播放恢复动画，已恢复行动的目标直接恢复架势。
        /// </summary>
        public void ExpireExecutionWindow()
        {
            if (!m_LockDuringWaiting || !enabled || m_Phase != PostureBreakPhase.Waiting)
            {
                m_Posture.Restore();
                return;
            }

            PlayEnd();
        }

        void PlayLoop()
        {
            m_Phase = PostureBreakPhase.Waiting;

            if (!m_LockDuringWaiting)
            {
                ReturnToDefaultState();
                return;
            }

            PlayAnimation(m_Loop);
        }

        void PlayEnd()
        {
            m_Phase = PostureBreakPhase.Ending;
            PlayWithEvents(m_End, FinishBreak);
        }

        void FinishBreak()
        {
            m_Posture.Restore();
            ReturnToDefaultState();
        }
    }
}
