using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 角色独立架势资源，负责受损、恢复与破韧周期。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Posture : MonoBehaviour, IConfigReceiver<PostureConfig>
    {
        float m_CurrentPosture;
        float m_MaxPosture;
        float m_RecoveryDelay;
        float m_RecoveryPerSecond;
        float m_RecoveryRemaining;
        IEventBus m_EventBus;

        public float CurrentPosture => m_CurrentPosture;
        public float MaxPosture => m_MaxPosture;
        public bool IsBroken => m_CurrentPosture <= 0f;

        void Awake()
        {
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
        }

        void Update()
        {
            TickRecovery(Time.deltaTime);
        }

        /// <summary>
        /// 使用角色配置初始化架势资源。
        /// </summary>
        public void Initialize(PostureConfig config)
        {
            m_MaxPosture = config.MaxPosture;
            m_RecoveryDelay = config.RecoveryDelay;
            m_RecoveryPerSecond = config.RecoveryPerSecond;
            m_CurrentPosture = m_MaxPosture;
            m_RecoveryRemaining = 0f;
            PublishChanged(false);
        }

        /// <summary>
        /// 结算架势伤害并返回实际扣除值。
        /// </summary>
        public float ApplyDamage(float damage)
        {
            if (damage <= 0f || IsBroken)
                return 0f;

            float appliedDamage = Mathf.Min(m_CurrentPosture, damage);
            m_CurrentPosture -= appliedDamage;
            m_RecoveryRemaining = m_RecoveryDelay;
            PublishChanged(IsBroken);
            return appliedDamage;
        }

        /// <summary>
        /// 恢复全部架势并开始新的破韧周期。
        /// </summary>
        public void Restore()
        {
            m_CurrentPosture = m_MaxPosture;
            m_RecoveryRemaining = 0f;
            PublishChanged(false);
        }

        void TickRecovery(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            if (IsBroken)
                return;

            if (m_CurrentPosture >= m_MaxPosture)
                return;

            if (m_RecoveryRemaining > 0f)
            {
                if (m_RecoveryRemaining >= deltaTime)
                {
                    m_RecoveryRemaining -= deltaTime;
                    return;
                }

                deltaTime -= m_RecoveryRemaining;
                m_RecoveryRemaining = 0f;
            }

            if (m_RecoveryPerSecond <= 0f)
                return;

            m_CurrentPosture = Mathf.Min(
                m_MaxPosture,
                m_CurrentPosture + m_RecoveryPerSecond * deltaTime);
            PublishChanged(false);
        }

        /// <summary>
        /// 发布架势变化
        /// </summary>
        /// <param name="isNewlyBroken">本次变化是否让架势从有值变为零；恢复或普通扣减时为 false。</param>
        void PublishChanged(bool isNewlyBroken)
        {
            m_EventBus.Publish(this, new PostureChangedEvent(this, isNewlyBroken));
        }
    }
}
