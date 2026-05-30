using Animancer;
using Animancer.FSM;
using MotionCore.Gameplay.Common;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
#endif

namespace MotionCore.Gameplay.Character
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class Character : MonoBehaviour
    {
        [SerializeField] AnimancerComponent m_Animancer;
        public AnimancerComponent Animancer => m_Animancer;

        [SerializeField] Transform m_FacingRoot;
        public Transform FacingRoot => m_FacingRoot;

        [SerializeField] StateMachine<CharacterState>.WithDefault m_StateMachine = new();
        public StateMachine<CharacterState>.WithDefault StateMachine => m_StateMachine;

        [SerializeField] CharacterDefinition m_CharacterDefinition;
        [SerializeField] HealthConfig m_Health = new();
        [SerializeField] MonoBehaviour[] m_ConfigReceivers = System.Array.Empty<MonoBehaviour>();

        CharacterParameters m_Parameters;
        public CharacterParameters Parameters => m_Parameters;

        void Awake()
        {
            m_StateMachine.InitializeAfterDeserialize();
            m_Parameters = new CharacterParameters();
        }

        void Start()
        {
            for (int i = 0; i < m_ConfigReceivers.Length; i++)
            {
                MonoBehaviour receiver = m_ConfigReceivers[i];
                if (receiver is IConfigReceiver<CharacterDefinition> characterReceiver)
                    characterReceiver.Initialize(m_CharacterDefinition);
                if (receiver is IConfigReceiver<HealthConfig> healthReceiver)
                    healthReceiver.Initialize(m_Health);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            IConfigReceiver<CharacterDefinition>[] characterReceivers =
                GetComponentsInChildren<IConfigReceiver<CharacterDefinition>>(true);
            IConfigReceiver<HealthConfig>[] healthReceivers =
                GetComponentsInChildren<IConfigReceiver<HealthConfig>>(true);

            HashSet<MonoBehaviour> receivers = new();
            for (int i = 0; i < characterReceivers.Length; i++)
                receivers.Add((MonoBehaviour)characterReceivers[i]);
            for (int i = 0; i < healthReceivers.Length; i++)
                receivers.Add((MonoBehaviour)healthReceivers[i]);

            m_ConfigReceivers = new List<MonoBehaviour>(receivers).ToArray();
        }
#endif
    }
}
