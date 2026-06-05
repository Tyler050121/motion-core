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
    }
}
