using System;
using MotionCore.Gameplay.Character;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [Serializable]
    public struct AttackHitDefinition
    {
        [SerializeField] HitProfile m_Profile;
        public HitProfile Profile => m_Profile;

        [SerializeField] CharacterAnchor m_Anchor;
        public CharacterAnchor Anchor => m_Anchor;

        [SerializeField] Vector3 m_LocalOffset;
        public Vector3 LocalOffset => m_LocalOffset;

        [SerializeField, Tooltip("该段攻击附带的特效")]
        AttackVfxDefinition m_Vfx;
        public AttackVfxDefinition Vfx => m_Vfx;
    }
}
