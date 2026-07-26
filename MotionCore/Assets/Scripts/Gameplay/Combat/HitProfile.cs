using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MotionCore.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "MotionCore/Combat/Hit Profile")]
    public sealed class HitProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] float m_Damage = 10f;
        public float Damage => m_Damage;

        [SerializeField, Min(0.01f)] float m_Radius = 0.75f;
        public float Radius => m_Radius;

        // 只查 Hurtbox 层，不要放开到全层：全层会把环境碰撞体也捞进 Overlap 结果。
        [SerializeField, Tooltip("命中查询层，仅勾 Hurtbox")] LayerMask m_TargetLayers;
        public LayerMask TargetLayers => m_TargetLayers;

        [SerializeField, Min(0f), Tooltip("受击僵直强度")] float m_StaggerPower = 0.18f;
        public float StaggerPower => m_StaggerPower;

        [SerializeField, Min(0f), Tooltip("击退力度")] float m_KnockbackPower;
        public float KnockbackPower => m_KnockbackPower;

        [SerializeField, FormerlySerializedAs("m_Vfx")] HitVfxDefinition[] m_Vfx = Array.Empty<HitVfxDefinition>();
        public IReadOnlyList<HitVfxDefinition> Vfx => m_Vfx;

        // [SerializeField, Min(0f)] float m_HitStopSeconds;
        // public float HitStopSeconds => m_HitStopSeconds;
    }
}
