using Animancer;
using Animancer.FSM;
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

        [SerializeField] StateMachine<CharacterState>.WithDefault m_StateMachine = new();
        public StateMachine<CharacterState>.WithDefault StateMachine => m_StateMachine;

        CharacterParameters m_Parameters;
        public CharacterParameters Parameters => m_Parameters;

        void Awake()
        {
            m_StateMachine.InitializeAfterDeserialize();
            m_Parameters = new CharacterParameters();
        }
    }
}
