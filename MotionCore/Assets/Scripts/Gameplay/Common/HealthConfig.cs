using UnityEngine;

namespace MotionCore.Gameplay.Common
{
    [System.Serializable]
    public sealed class HealthConfig
    {
        [SerializeField, Min(1f)] float m_MaxHealth = 100f;
        public float MaxHealth => m_MaxHealth;
    }
}
