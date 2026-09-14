using System;
using Animancer;
using Animancer.FSM;
using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Combat;
using MotionCore.Gameplay.Configs;
using UnityEngine;

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

        CharacterParameters m_Parameters;
        public CharacterParameters Parameters => m_Parameters;
        CharacterController m_Controller;
        int m_CharacterCollisionLayers;

        void Awake()
        {
            m_StateMachine.InitializeAfterDeserialize();
            m_Parameters = new CharacterParameters();
            m_Controller = GetComponent<CharacterController>();
            m_CharacterCollisionLayers = LayerMask.GetMask(GlobalConfig.LayerNames.Character);
        }

        /// <summary>
        /// 初始化角色运行参数。
        /// </summary>
        public void Initialize(CharacterStat stat)
        {
            GetComponent<Health>().Initialize(stat);
            GetComponent<Posture>().Initialize(stat);
            GetComponentInChildren<CharacterRootMotionMotor>(true).Initialize(stat);
            GetComponentInChildren<CharacterCommandController>(true).Initialize(m_CharacterDefinition, stat);
        }

        /// <summary>
        /// 获取必需的角色挂点。
        /// </summary>
        public Transform GetAnchor(CharacterAnchor anchor)
        {
            for (int i = 0; i < m_Anchors.Length; i++)
            {
                AnchorBinding binding = m_Anchors[i];
                if (binding.Anchor == anchor && binding.Source != null)
                    return binding.Source;
            }

            throw new InvalidOperationException($"角色挂点未配置：{name}/{anchor}");
        }

        /// <summary>
        /// 开关角色身体碰撞。
        /// </summary>
        public void SetCollisionEnabled(bool enabled)
        {
            if (enabled)
                m_Controller.excludeLayers &= ~m_CharacterCollisionLayers;
            else
                m_Controller.excludeLayers |= m_CharacterCollisionLayers;
        }

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
