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

        [SerializeField] Vector3 m_LocalOffset = new(0f, 1f, 1f);
        public Vector3 LocalOffset => m_LocalOffset;

        [SerializeField] LayerMask m_TargetLayers = ~0;
        public LayerMask TargetLayers => m_TargetLayers;

        // [SerializeField, Min(0f)] float m_HitStopSeconds;
        // public float HitStopSeconds => m_HitStopSeconds;

        // [SerializeField, Min(0f)] float m_StaggerSeconds;
        // public float StaggerSeconds => m_StaggerSeconds;
    }
}
