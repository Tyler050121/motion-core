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

        IDamageable m_Damageable;
        IEventBus m_EventBus;
        Faction m_Faction;
        bool m_Invulnerable;

        public IDamageable Damageable => m_Damageable;

        /// <summary>
        /// 所属阵营，启动时从角色的 LockOnTarget 取一次缓存。
        /// </summary>
        public Faction Faction => m_Faction;

        public Vector3 ResolveVisualPoint(Transform attacker)
        {
            Vector3 toAttacker = attacker.root.position - transform.position;
            toAttacker.y = 0f;
            return transform.position
                + toAttacker.normalized * m_VisualForwardOffset
                + Vector3.up * m_VisualHeightOffset;
        }

        public void SetInvulnerable(bool invulnerable)
        {
            m_Invulnerable = invulnerable;
        }

        /// <summary>
        /// 结算一次命中。无敌期间拒绝伤害并广播 HitAvoidedEvent，不广播 HitEvent；
        /// 调用方（MeleeHitbox）仍会把本次命中记入攻击窗口。
        /// </summary>
        public void ReceiveHit(HitProfile profile, Vector3 point, Vector3 visualPoint, Vector3 direction, Transform attacker)
        {
            if (m_Invulnerable)
            {
                m_EventBus.Publish(this, new HitAvoidedEvent());
                return;
            }

            m_Damageable.ApplyDamage(profile.Damage);
            HitFeedbackContext feedback = new(point, visualPoint, direction, profile.Vfx);
            HitResult result = new(
                this,
                attacker,
                profile.Damage,
                m_Damageable.CurrentHealth,
                m_Damageable.IsDepleted,
                profile.KnockbackPower);
            m_EventBus.Publish(m_Damageable, new HitEvent(result, feedback));
        }

        void Awake()
        {
            m_Damageable = GetComponentInParent<IDamageable>();
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
