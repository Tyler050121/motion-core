using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Common
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable, IConfigReceiver<HealthConfig>
    {
        [SerializeField, ReadOnly] float m_MaxHealth = 100f;

        float m_CurrentHealth;

        public float CurrentHealth => m_CurrentHealth;
        public bool IsDepleted => m_CurrentHealth <= 0f;

        // TODO: 此处 Awake 留给没有走 IConfigReceiver 初始化的情况，之后可以考虑去掉
        void Awake()
        {
            m_CurrentHealth = m_MaxHealth;
        }

        public void Initialize(HealthConfig config)
        {
            m_MaxHealth = config.MaxHealth;
            m_CurrentHealth = m_MaxHealth;
        }

        public void ApplyDamage(float damage)
        {
            m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - damage);
        }
    }
}
