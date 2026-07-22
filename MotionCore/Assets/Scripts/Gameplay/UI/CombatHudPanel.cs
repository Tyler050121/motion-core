using MotionCore.Gameplay.Character;
using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.UI
{
    [DisallowMultipleComponent]
    public sealed class CombatHudPanel : MonoBehaviour,
                                         IEventListener<HealthChangedEvent>,
                                         IEventListener<PlayerSpawnedEvent>
    {
        [SerializeField, Tooltip("玩家血条")] HealthBar m_HealthBar;

        Health m_PlayerHealth;
        IEventBus m_EventBus;

        void Awake()
        {
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
        }

        /// <summary>
        /// 监听玩家生成。
        /// </summary>
        void OnEnable()
        {
            m_EventBus.Subscribe<PlayerSpawnedEvent>(this);
        }

        /// <summary>
        /// 停止监听玩家。
        /// </summary>
        void OnDisable()
        {
            m_EventBus.Unsubscribe<PlayerSpawnedEvent>(this);
            UnbindPlayer();
        }

        /// <summary>
        /// 绑定新生成的玩家。
        /// </summary>
        public void OnEvent(PlayerSpawnedEvent eventData)
        {
            UnbindPlayer();
            m_PlayerHealth = eventData.Health;
            m_HealthBar.SetHealthImmediate(m_PlayerHealth.CurrentHealth, m_PlayerHealth.MaxHealth);
            m_EventBus.Subscribe<HealthChangedEvent>(m_PlayerHealth, this);
        }

        /// <summary>
        /// 刷新当前玩家血条。
        /// </summary>
        public void OnEvent(HealthChangedEvent eventData)
        {
            m_HealthBar.SetHealth(eventData.CurrentHealth, eventData.MaxHealth);
        }

        void UnbindPlayer()
        {
            if (object.ReferenceEquals(m_PlayerHealth, null))
                return;

            m_EventBus.Unsubscribe<HealthChangedEvent>(m_PlayerHealth, this);
            m_PlayerHealth = null;
        }
    }
}
