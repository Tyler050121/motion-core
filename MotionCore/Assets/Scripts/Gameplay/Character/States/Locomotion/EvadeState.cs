using Animancer;
using MotionCore.Gameplay.Combat;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    public sealed class EvadeState : CharacterState
    {
        [SerializeField] TransitionAsset m_EvadeFront;
        [SerializeField] TransitionAsset m_EvadeBack;
        [SerializeField] Hurtbox m_Hurtbox;
        [SerializeField] CharacterController m_Controller;
        [SerializeField, Tooltip("闪避期间忽略的角色实体碰撞层")] LayerMask m_CharacterCollisionLayers;

        protected override bool CanInterruptSelf => (ExitOptions & CharacterStateExitOptions.Evade) != 0;
        public override CharacterStateType Type => CharacterStateType.Evade;

        void OnEnable()
        {
            TransitionAsset evade = Character.Parameters.HasMoveInput ? m_EvadeFront : m_EvadeBack;
            // 闪避自身不享受 Default（Default 含 Evade，会导致刚起步就能连续翻滚）
            // 连续闪避由动画上的 CanEvade 事件开启
            ExitOptions = CharacterStateExitOptions.None;

            AnimancerState state = Character.Animancer.Play(evade);
            bool isNewEventSequence = state.Events(this, out AnimancerEvent.Sequence events);

            if (isNewEventSequence)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    string eventName = events.GetName(i);
                    if (eventName == GlobalConfig.AnimationEventNames.CanCancel)
                        events.SetCallback(i, OpenCancel);
                    else if (eventName == GlobalConfig.AnimationEventNames.CanEvade)
                        events.SetCallback(i, OpenEvade);
                    else if (eventName == GlobalConfig.AnimationEventNames.InvulnerableStart)
                        events.SetCallback(i, OpenInvulnerability);
                    else if (eventName == GlobalConfig.AnimationEventNames.InvulnerableEnd)
                        events.SetCallback(i, CloseInvulnerability);
                    else if (eventName == GlobalConfig.AnimationEventNames.CharacterCollisionOff)
                        events.SetCallback(i, DisableCharacterCollision);
                    else if (eventName == GlobalConfig.AnimationEventNames.CharacterCollisionOn)
                        events.SetCallback(i, RestoreCharacterCollision);
                }
            }

            events.OnEnd = ReturnToDefaultState;
        }

        void OnDisable()
        {
            CloseInvulnerability();
            RestoreCharacterCollision();
        }

        void OpenInvulnerability() => m_Hurtbox.SetInvulnerable(true);
        void CloseInvulnerability() => m_Hurtbox.SetInvulnerable(false);

        // 只增删自己负责的位，不整体覆盖 excludeLayers，避免抹掉其他系统的修改。
        void DisableCharacterCollision()
        {
            m_Controller.excludeLayers |= m_CharacterCollisionLayers.value;
        }

        void RestoreCharacterCollision()
        {
            m_Controller.excludeLayers &= ~m_CharacterCollisionLayers.value;
        }
    }
}
