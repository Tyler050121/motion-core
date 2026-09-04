using System;
using Animancer;
using Animancer.FSM;
using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Combat;
using UnityEngine;
using MotionCore.Infrastructure;
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

        [SerializeField, Tooltip("角色挂点绑定")]
        AnchorBinding[] m_Anchors = Array.Empty<AnchorBinding>();

        [SerializeField] StateMachine<CharacterState>.WithDefault m_StateMachine = new();
        public StateMachine<CharacterState>.WithDefault StateMachine => m_StateMachine;

        [SerializeField] CharacterDefinition m_CharacterDefinition;
        [SerializeField] HealthConfig m_Health = new();
        [SerializeField] PostureConfig m_Posture = new();
        [SerializeField, ReadOnly] MonoBehaviour[] m_ConfigReceivers = System.Array.Empty<MonoBehaviour>();

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
                if (receiver is IConfigReceiver<PostureConfig> postureReceiver)
                    postureReceiver.Initialize(m_Posture);
            }
        }

        public bool TryGetAnchor(CharacterAnchor anchor, out Transform source)
        {
            for (int i = 0; i < m_Anchors.Length; i++)
            {
                AnchorBinding binding = m_Anchors[i];
                if (binding.Anchor != anchor)
                    continue;

                source = binding.Source;
                if (source != null)
                    return true;

                Debug.LogError($"角色挂点 {anchor} 未绑定 Transform。", this);
                return false;
            }

            source = null;
            Debug.LogError($"角色挂点 {anchor} 未配置。", this);
            return false;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            IConfigReceiver<CharacterDefinition>[] characterReceivers =
                GetComponentsInChildren<IConfigReceiver<CharacterDefinition>>(true);
            IConfigReceiver<HealthConfig>[] healthReceivers =
                GetComponentsInChildren<IConfigReceiver<HealthConfig>>(true);
            IConfigReceiver<PostureConfig>[] postureReceivers =
                GetComponentsInChildren<IConfigReceiver<PostureConfig>>(true);

            HashSet<MonoBehaviour> receivers = new();
            for (int i = 0; i < characterReceivers.Length; i++)
                receivers.Add((MonoBehaviour)characterReceivers[i]);
            for (int i = 0; i < healthReceivers.Length; i++)
                receivers.Add((MonoBehaviour)healthReceivers[i]);
            for (int i = 0; i < postureReceivers.Length; i++)
                receivers.Add((MonoBehaviour)postureReceivers[i]);

            m_ConfigReceivers = new List<MonoBehaviour>(receivers).ToArray();
        }
#endif

        [Serializable]
        struct AnchorBinding
        {
            [SerializeField] CharacterAnchor m_Anchor;
            public CharacterAnchor Anchor => m_Anchor;

            [SerializeField] Transform m_Source;
            public Transform Source => m_Source;
        }
    }
}
