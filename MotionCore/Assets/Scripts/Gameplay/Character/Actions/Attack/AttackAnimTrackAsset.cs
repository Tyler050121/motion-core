using System;
using Animancer;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 攻击专用动画轨道，负责把动画事件参数映射到命中配置。
    /// </summary>
    [CreateAssetMenu(menuName = "MotionCore/Combat/Attack Anim Track")]
    public sealed class AttackAnimTrackAsset : ScriptableObject
    {
        [SerializeField, Tooltip("播放动画")]
        TransitionAsset m_Animation;
        public TransitionAsset Animation => m_Animation;

        [SerializeField] HitProfile[] m_HitProfiles = Array.Empty<HitProfile>();

        /// <summary>
        /// 按动画事件参数读取对应的命中配置。
        /// </summary>
        public HitProfile GetHitProfile(int index)
        {
            return m_HitProfiles[index];
        }
    }
}
