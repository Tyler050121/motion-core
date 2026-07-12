using UnityEngine;
using UnityEngine.UI;

namespace MotionCore.Gameplay.UI
{
    public sealed class HealthBar : Bar
    {
        [SerializeField, Tooltip("受伤段图形")] Image m_DamageFill;
        [SerializeField, Tooltip("回血段图形")] Image m_HealFill;
        [SerializeField, Min(0f), Tooltip("受伤段停留时间")] float m_DamageDelay = 0.25f;
        [SerializeField, Min(0.01f), Tooltip("受伤段扣除时间")] float m_DamageDuration = 0.35f;
        [SerializeField, Min(0f), Tooltip("回血段停留时间")] float m_HealDelay = 0.15f;
        [SerializeField, Min(0.01f), Tooltip("回血段填充时间")] float m_HealDuration = 0.25f;
        [SerializeField, Min(0f), Tooltip("测试扣血量")] float m_TestStep = 10f;

        float m_CurrentHealth = 100f;
        float m_MaxHealth = 100f;
        float m_DamageValue = 1f;
        float m_HealValue = 1f;
        float m_DamageDelayRemaining;
        float m_HealDelayRemaining;

        void Awake()
        {
            SetHealth(m_CurrentHealth, m_MaxHealth);
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Minus))
                SetHealth(m_CurrentHealth - m_TestStep, m_MaxHealth);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Equals))
                SetHealth(m_CurrentHealth + m_TestStep, m_MaxHealth);

            if (Value < m_HealValue)
            {
                if (m_HealDelayRemaining > 0f)
                {
                    m_HealDelayRemaining -= Time.unscaledDeltaTime;
                    return;
                }

                float value = Mathf.MoveTowards(Value, m_HealValue, Time.unscaledDeltaTime / m_HealDuration);
                SetValue(value);
                if (value >= m_HealValue)
                {
                    m_DamageValue = value;
                    ResizeBar(m_DamageFill.rectTransform, m_DamageValue);
                }
                return;
            }

            if (m_DamageValue <= Value)
                return;
            if (m_DamageDelayRemaining > 0f)
            {
                m_DamageDelayRemaining -= Time.unscaledDeltaTime;
                return;
            }

            m_DamageValue = Mathf.MoveTowards(m_DamageValue, Value, Time.unscaledDeltaTime / m_DamageDuration);
            ResizeBar(m_DamageFill.rectTransform, m_DamageValue);
        }

        /// <summary>
        /// 刷新血量并保留本次受伤段。
        /// </summary>
        public void SetHealth(float currentHealth, float maxHealth)
        {
            float previousValue = Value;
            m_MaxHealth = Mathf.Max(1f, maxHealth);
            m_CurrentHealth = Mathf.Clamp(currentHealth, 0f, m_MaxHealth);

            float value = m_CurrentHealth / m_MaxHealth;
            if (value > previousValue)
            {
                m_HealValue = value;
                m_DamageValue = previousValue;
                m_HealDelayRemaining = m_HealDelay;
                ResizeBar(m_HealFill.rectTransform, m_HealValue);
                ResizeBar(m_DamageFill.rectTransform, m_DamageValue);
                return;
            }

            SetValue(value);
            m_HealValue = value;
            ResizeBar(m_HealFill.rectTransform, m_HealValue);

            if (value == previousValue)
                m_DamageValue = value;
            else
                m_DamageDelayRemaining = m_DamageDelay;

            ResizeBar(m_DamageFill.rectTransform, m_DamageValue);
        }
    }
}
