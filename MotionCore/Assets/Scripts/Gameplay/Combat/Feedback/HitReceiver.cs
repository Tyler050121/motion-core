using System.Collections.Generic;
using DamageNumbersPro;
using MotionCore.Gameplay.Character;
using MotionCore.Gameplay.Common;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Hurtbox))]
    public sealed class HitReceiver : MonoBehaviour, IEventListener<HitEvent>
    {
        [SerializeField, Tooltip("受击弹字预设")] DamageNumber m_DamageNumber;
        [SerializeField, Tooltip("弹字生成偏移")] Vector3 m_WorldOffset = new(0f, 1f, 0f);
        [SerializeField, Tooltip("弹字跟随受击目标")] bool m_FollowTarget = true;

        Hurtbox m_Hurtbox;
        Health m_Health;
        IEventBus m_EventBus;
        IHitReactionHandler m_ReactionHandler; // 非角色目标可以没有受击状态处理器
        IHitVfxService m_HitVfx;
        readonly List<StaggerLevel> m_PendingStaggerLevels = new();
        readonly List<float> m_PendingKnockbackPowers = new();

        void Awake()
        {
            m_Hurtbox = GetComponent<Hurtbox>();
            m_Health = GetComponentInParent<Health>();
            m_EventBus = ServiceLocator.Resolve<IEventBus>();
            m_ReactionHandler = transform.parent.GetComponentInChildren<IHitReactionHandler>();
            m_HitVfx = ServiceLocator.Resolve<IHitVfxService>();
        }

        void OnEnable()
        {
            m_EventBus.Subscribe<HitEvent>(m_Health, this);
        }

        void OnDisable()
        {
            m_EventBus.Unsubscribe<HitEvent>(m_Health, this);
            m_PendingStaggerLevels.Clear();
            m_PendingKnockbackPowers.Clear();
        }

        void LateUpdate()
        {
            if (m_PendingKnockbackPowers.Count == 0)
                return;

            StaggerLevel staggerLevel = m_PendingStaggerLevels[0];
            float knockbackPower = m_PendingKnockbackPowers[0];
            for (int i = 1; i < m_PendingKnockbackPowers.Count; i++)
            {
                if (m_PendingStaggerLevels[i] > staggerLevel)
                    staggerLevel = m_PendingStaggerLevels[i];

                knockbackPower = Mathf.Max(knockbackPower, m_PendingKnockbackPowers[i]);
            }

            m_ReactionHandler?.ReceiveHit(staggerLevel, knockbackPower);

            m_PendingStaggerLevels.Clear();
            m_PendingKnockbackPowers.Clear();
        }

        public void OnEvent(HitEvent hitEvent)
        {
            if (hitEvent.Result.Hurtbox != m_Hurtbox)
                return;

            HitResult result = hitEvent.Result;
            HitFeedbackContext feedback = hitEvent.Feedback;
            ShowDamageNumber(feedback.Point, result.Damage, result.Hurtbox.transform);
            m_HitVfx.Play(feedback);
            if (m_Health.IsDead)
                return;

            m_PendingStaggerLevels.Add(result.StaggerLevel);
            m_PendingKnockbackPowers.Add(result.KnockbackPower);
        }

        void ShowDamageNumber(Vector3 point, float damage, Transform target)
        {
            DamageNumber popup = m_DamageNumber.Spawn(point + m_WorldOffset, damage);
            if (m_FollowTarget)
                popup.SetFollowedTarget(target);
        }
    }
}
