using Animancer;
using Animancer.FSM;
using MotionCore.Gameplay;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class Character : MonoBehaviour, IDamageable
    {
        [SerializeField] AnimancerComponent m_Animancer;
        public AnimancerComponent Animancer => m_Animancer;

        [SerializeField] Transform m_FacingRoot;
        public Transform FacingRoot => m_FacingRoot;

        [SerializeField] StateMachine<CharacterState>.WithDefault m_StateMachine = new();
        public StateMachine<CharacterState>.WithDefault StateMachine => m_StateMachine;

        [SerializeField, Min(1f)] float m_MaxHealth = 100f;

        CharacterParameters m_Parameters;
        public CharacterParameters Parameters => m_Parameters;

        public float CurrentHealth => m_Parameters.CurrentHealth;
        bool IDamageable.IsDepleted => m_Parameters.IsDead;

        void Awake()
        {
            m_StateMachine.InitializeAfterDeserialize();
            m_Parameters = new CharacterParameters();
            m_Parameters.ResetHealth(m_MaxHealth);
        }

        void IDamageable.ApplyDamage(float damage) => m_Parameters.ApplyDamage(damage);
    }
}
