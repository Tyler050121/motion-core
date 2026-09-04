using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 播放死亡动画并永久锁定角色状态。
    /// </summary>
    public sealed class DeadState : CharacterState
    {
        [SerializeField, Tooltip("死亡动画")] TransitionAsset m_Death;

        public override CharacterStateType Type => CharacterStateType.Dead;
        public override CastPriority CurrentCastPriority => CastPriority.None;
        public override StaggerLevel CurrentStaggerLevel => StaggerLevel.Execution;

        void OnEnable()
        {
            PlayAnimation(m_Death);
        }
    }
}
