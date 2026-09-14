using MotionCore.Gameplay.Common;
using MotionCore.Gameplay.Targeting;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("表现点朝攻击者偏移")] float m_VisualForwardOffset = 0.8f;
        [SerializeField, Min(0f), Tooltip("表现点向上偏移")] float m_VisualHeightOffset = 1f;

        Health m_Health;
        Posture m_Posture;
        IEventBus m_EventBus;
        Faction m_Faction;
        bool m_Invulnerable;
        bool m_Parrying;
        float m_DamageMultiplier = 1f;
        float m_PostureDamageMultiplier = 1f;

        /// <summary>
        /// 所属阵营，启动时从角色的 LockOnTarget 取一次缓存。
        /// </summary>
        public Faction Faction => m_Faction;
        public Health Health => m_Health;

        public Vector3 ResolveVisualPoint(Transform attacker)
        {
            Vector3 toAttacker = attacker.position - transform.position;
            toAttacker.y = 0f;
            return transform.position
                + toAttacker.normalized * m_VisualForwardOffset
                + Vector3.up * m_VisualHeightOffset;
        }

        public void SetInvulnerable(bool invulnerable)
        {
            m_Invulnerable = invulnerable;
        }

        public void SetParrying(bool parrying)
        {
            m_Parrying = parrying;
        }

        public void SetDamageMultiplier(float multiplier)
        {
            m_DamageMultiplier = multiplier;
        }

        public void SetPostureDamageMultiplier(float multiplier)
        {
            m_PostureDamageMultiplier = multiplier;
        }

        /// <summary>
        /// 结算一次命中。卸势或无敌期间拒绝伤害并广播对应事件，不广播 HitEvent；
        /// 调用方（MeleeHitbox）仍会把本次命中记入攻击窗口。
        /// </summary>
        public void ReceiveHit(HitProfile profile, Vector3 point, Vector3 visualPoint, Vector3 direction, Transform attacker)
        {
            if (m_Health.IsDead)
                return;

            if (m_Parrying)
            {
                m_EventBus.Publish(this, new HitParriedEvent(attacker));
                return;
            }

            if (m_Invulnerable)
            {
                m_EventBus.Publish(this, new HitAvoidedEvent());
                return;
            }

            float damage = profile.Damage * m_DamageMultiplier;
            m_Posture?.ApplyDamage(profile.PostureDamage * m_PostureDamageMultiplier);
            m_Health.ApplyDamage(damage);
            HitFeedbackContext feedback = new(point, visualPoint, direction, profile.Vfx);
            HitResult result = new(
                this,
                attacker,
                damage,
                profile.StaggerLevel,
                profile.KnockbackPower);
            m_EventBus.Publish(m_Health, new HitEvent(result, feedback));
        }

        void Awake()
        {
            m_Health = GetComponentInParent<Health>();
            m_Posture = GetComponentInParent<Posture>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
            m_Faction = GetComponentInParent<LockOnTarget>().Faction;
        }

#if UNITY_EDITOR
        void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer(GlobalConfig.LayerNames.Hurtbox);
        }
#endif
    }
}
