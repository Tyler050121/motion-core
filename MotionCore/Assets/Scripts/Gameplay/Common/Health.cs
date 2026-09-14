using MotionCore.Gameplay.Configs;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Common
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        float m_MaxHealth;

        float m_CurrentHealth;
        IEventBus m_EventBus;
        bool m_IsInitialized;

        public float CurrentHealth => m_CurrentHealth;
        public float MaxHealth => m_MaxHealth;
        public bool IsDead => m_IsInitialized && m_CurrentHealth <= 0f;

        void Awake()
        {
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
        }

        /// <summary>
        /// 使用角色数值初始化生命资源。
        /// </summary>
        public void Initialize(CharacterStat stat)
        {
            m_MaxHealth = stat.MaxHp;
            m_CurrentHealth = m_MaxHealth;
            m_IsInitialized = true;
            m_EventBus.Publish(this, new HealthChangedEvent(this));
        }

        /// <summary>
        /// 结算伤害并发布生命变化。
        /// </summary>
        public void ApplyDamage(float damage)
        {
            m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - damage);
            m_EventBus.Publish(this, new HealthChangedEvent(this));
        }

        /// <summary>
        /// 恢复生命并发布生命变化。
        /// </summary>
        public void Heal(float healing)
        {
            m_CurrentHealth = Mathf.Min(m_MaxHealth, m_CurrentHealth + healing);
            m_EventBus.Publish(this, new HealthChangedEvent(this));
        }
    }
}
