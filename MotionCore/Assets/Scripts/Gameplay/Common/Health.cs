using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Common
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable, IConfigReceiver<HealthConfig>
    {
        [SerializeField, ReadOnly] float m_MaxHealth = 100f;

        float m_CurrentHealth;
        IEventBus m_EventBus;

        public float CurrentHealth => m_CurrentHealth;
        public float MaxHealth => m_MaxHealth;
        public bool IsDepleted => m_CurrentHealth <= 0f;

        // TODO: 此处 Awake 留给没有走 IConfigReceiver 初始化的情况，之后可以考虑去掉
        void Awake()
        {
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
            m_CurrentHealth = m_MaxHealth;
        }

        /// <summary>
        /// 使用角色配置初始化生命值。
        /// </summary>
        public void Initialize(HealthConfig config)
        {
            m_MaxHealth = config.MaxHealth;
            m_CurrentHealth = m_MaxHealth;
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
