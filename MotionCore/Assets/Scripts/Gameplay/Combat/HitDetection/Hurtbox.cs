using MotionCore.Gameplay;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class Hurtbox : MonoBehaviour
    {
        IDamageable m_Damageable;

        public IDamageable Damageable => m_Damageable;

        public HitResult ReceiveHit(HitProfile profile, Vector3 point)
        {
            m_Damageable.ApplyDamage(profile.Damage);
            return new HitResult(
                this,
                profile.Damage,
                m_Damageable.CurrentHealth,
                m_Damageable.IsDepleted,
                point);
        }

        void Awake()
        {
            m_Damageable = GetComponentInParent<IDamageable>();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            m_Damageable = GetComponentInParent<IDamageable>();
        }
#endif
    }
}
