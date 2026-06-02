using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "MotionCore/Combat/Hit Profile")]
    public sealed class HitProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] float m_Damage = 10f;
        public float Damage => m_Damage;

        [SerializeField, Min(0.01f)] float m_Radius = 0.75f;
        public float Radius => m_Radius;

        [SerializeField] LayerMask m_TargetLayers = ~0;
        public LayerMask TargetLayers => m_TargetLayers;

        [SerializeField, Min(0f), Tooltip("受击僵直强度")] float m_StaggerPower = 0.18f;
        public float StaggerPower => m_StaggerPower;

        [SerializeField, Min(0f), Tooltip("击退力度")] float m_KnockbackPower;
        public float KnockbackPower => m_KnockbackPower;

        // [SerializeField, Min(0f)] float m_HitStopSeconds;
        // public float HitStopSeconds => m_HitStopSeconds;
    }
}
