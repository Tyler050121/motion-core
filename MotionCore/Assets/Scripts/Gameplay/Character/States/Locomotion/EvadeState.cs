using Animancer;
using MotionCore.Gameplay.Combat;
using MotionCore.Infrastructure;
using UnityEngine;
using EventNames = MotionCore.GlobalConfig.AnimationEventNames;

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
            PlayWithEvents(evade, ReturnToDefaultState);
        }

        protected override void BindEvent(AnimancerEvent.Sequence events, int index, string name)
        {
            if (name == EventNames.InvulnerableStart)
                Bind(events, index, OpenInvulnerability);
            else if (name == EventNames.InvulnerableEnd)
                Bind(events, index, CloseInvulnerability);
            else if (name == EventNames.CharacterCollisionOff)
                Bind(events, index, DisableCharacterCollision);
            else if (name == EventNames.CharacterCollisionOn)
                Bind(events, index, RestoreCharacterCollision);
            else
                base.BindEvent(events, index, name);
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
