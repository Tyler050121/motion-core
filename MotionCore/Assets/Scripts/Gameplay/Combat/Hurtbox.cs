using System;
using MotionCore.Gameplay.Common;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("表现点朝攻击者偏移")] float m_VisualForwardOffset = 0.8f;
        [SerializeField, Min(0f), Tooltip("表现点向上偏移")] float m_VisualHeightOffset = 1f;

        IDamageable m_Damageable;

        public IDamageable Damageable => m_Damageable;
        public event Action<HitEvent> HitReceived;

        public Vector3 ResolveVisualPoint(Transform attacker)
        {
            Vector3 toAttacker = attacker.root.position - transform.position;
            toAttacker.y = 0f;
            return transform.position
                + toAttacker.normalized * m_VisualForwardOffset
                + Vector3.up * m_VisualHeightOffset;
        }

        public HitResult ReceiveHit(HitProfile profile, Vector3 point, Vector3 visualPoint, Vector3 direction)
        {
            m_Damageable.ApplyDamage(profile.Damage);
            HitFeedbackContext feedback = new(point, visualPoint, direction, profile.Vfx);
            HitResult result = new(
                this,
                profile.Damage,
                m_Damageable.CurrentHealth,
                m_Damageable.IsDepleted,
                profile.KnockbackPower);
            HitReceived?.Invoke(new HitEvent(result, feedback));
            return result;
        }

        void Awake()
        {
            m_Damageable = GetComponentInParent<IDamageable>();
        }
    }
}
