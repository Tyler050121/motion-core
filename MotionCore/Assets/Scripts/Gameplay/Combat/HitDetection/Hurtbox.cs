using System;
using MotionCore.Gameplay.Common;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class Hurtbox : MonoBehaviour
    {
        IDamageable m_Damageable;

        public IDamageable Damageable => m_Damageable;
        public event Action<HitResult> HitReceived;

        public HitResult ReceiveHit(HitProfile profile, Vector3 point)
        {
            m_Damageable.ApplyDamage(profile.Damage);
            HitResult result = new(
                this,
                profile.Damage,
                m_Damageable.CurrentHealth,
                m_Damageable.IsDepleted,
                point);
            HitReceived?.Invoke(result);
            return result;
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
