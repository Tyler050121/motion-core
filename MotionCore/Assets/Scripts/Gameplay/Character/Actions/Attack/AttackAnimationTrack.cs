using System;
using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 攻击专用动画轨道，负责把动画事件参数映射到命中配置。
    /// </summary>
    [CreateAssetMenu(menuName = "MotionCore/Character/Attack Animation Track")]
    public sealed class AttackAnimationTrack : ScriptableObject
    {
        [SerializeField, Tooltip("播放动画")]
        TransitionAsset m_Animation;
        public TransitionAsset Animation => m_Animation;

        [SerializeField] AttackHitDefinition[] m_Hits = Array.Empty<AttackHitDefinition>();

        /// <summary>
        /// 按动画事件参数读取对应的命中定义。
        /// </summary>
        public AttackHitDefinition GetHitDefinition(int index)
        {
            return m_Hits[index];
        }
    }
}
