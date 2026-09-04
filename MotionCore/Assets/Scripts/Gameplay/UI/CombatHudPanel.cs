using MotionCore.Gameplay.Character;
using MotionCore.Gameplay.Combat;
using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.UI
{
    [DisallowMultipleComponent]
    public sealed class CombatHudPanel : MonoBehaviour,
                                         IEventListener<HealthChangedEvent>,
                                         IEventListener<PostureChangedEvent>,
                                         IEventListener<PlayerSpawnedEvent>,
                                         IEventListener<ExecutionAvailabilityChangedEvent>
    {
        [SerializeField, Tooltip("玩家血条")] HealthBar m_HealthBar;
        [SerializeField, Tooltip("玩家架势条")] PostureBar m_PostureBar;
        [SerializeField, Tooltip("处决提示")] GameObject m_ExecutionPrompt;

        Health m_PlayerHealth;
        Posture m_PlayerPosture;
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
            m_EventBus.Subscribe<ExecutionAvailabilityChangedEvent>(this);
        }

        /// <summary>
        /// 停止监听玩家。
        /// </summary>
        void OnDisable()
        {
            m_EventBus.Unsubscribe<PlayerSpawnedEvent>(this);
            m_EventBus.Unsubscribe<ExecutionAvailabilityChangedEvent>(this);
            UnbindPlayer();
        }

        /// <summary>
        /// 绑定新生成的玩家。
        /// </summary>
        public void OnEvent(PlayerSpawnedEvent eventData)
        {
            UnbindPlayer();
            m_PlayerHealth = eventData.Health;
            m_PlayerPosture = eventData.Posture;

            m_HealthBar.SetHealthImmediate(m_PlayerHealth.CurrentHealth, m_PlayerHealth.MaxHealth);
            m_PostureBar.SetPostureImmediate(m_PlayerPosture.CurrentPosture, m_PlayerPosture.MaxPosture);

            m_EventBus.Subscribe<HealthChangedEvent>(m_PlayerHealth, this);
            m_EventBus.Subscribe<PostureChangedEvent>(m_PlayerPosture, this);
        }

        /// <summary>
        /// 刷新当前玩家血条。
        /// </summary>
        public void OnEvent(HealthChangedEvent eventData)
        {
            m_HealthBar.SetHealth(eventData.CurrentHealth, eventData.MaxHealth);
        }

        /// <summary>
        /// 刷新当前玩家架势条。
        /// </summary>
        public void OnEvent(PostureChangedEvent eventData)
        {
            m_PostureBar.SetPosture(eventData.CurrentPosture, eventData.MaxPosture);
        }

        /// <summary>
        /// 刷新处决按键提示。
        /// </summary>
        public void OnEvent(ExecutionAvailabilityChangedEvent eventData)
        {
            m_ExecutionPrompt.SetActive(eventData.IsAvailable);
        }

        void UnbindPlayer()
        {
            if (object.ReferenceEquals(m_PlayerHealth, null))
                return;

            m_EventBus.Unsubscribe<HealthChangedEvent>(m_PlayerHealth, this);
            m_EventBus.Unsubscribe<PostureChangedEvent>(m_PlayerPosture, this);
            m_PlayerHealth = null;
            m_PlayerPosture = null;
        }
    }
}
