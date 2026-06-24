using System.Collections.Generic;
using DamageNumbersPro;
using MotionCore.Infrastructure;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Hurtbox))]
    public sealed class HitReceiver : MonoBehaviour
    {
        [SerializeField, Tooltip("受击弹字预设")] DamageNumber m_DamageNumber;
        [SerializeField, Tooltip("弹字生成偏移")] Vector3 m_WorldOffset = new(0f, 1f, 0f);
        [SerializeField, Tooltip("弹字跟随受击目标")] bool m_FollowTarget = true;

        Hurtbox m_Hurtbox;
        IHitReactionHandler m_ReactionHandler; // 受击反应处理器
        IHitVfxService m_HitVfx;
        readonly List<float> m_PendingKnockbackPowers = new();

        void Awake()
        {
            m_Hurtbox = GetComponent<Hurtbox>();
            m_ReactionHandler = transform.parent.GetComponentInChildren<IHitReactionHandler>();
            m_HitVfx = ServiceLocator.Resolve<IHitVfxService>();
        }

        void OnEnable()
        {
            m_Hurtbox.HitReceived += Receive;
        }

        void OnDisable()
        {
            m_Hurtbox.HitReceived -= Receive;
            m_PendingKnockbackPowers.Clear();
        }

        void LateUpdate()
        {
            if (m_PendingKnockbackPowers.Count == 0)
                return;

            float knockbackPower = m_PendingKnockbackPowers[0];
            for (int i = 1; i < m_PendingKnockbackPowers.Count; i++)
                knockbackPower = Mathf.Max(knockbackPower, m_PendingKnockbackPowers[i]);

            m_ReactionHandler?.ReceiveHit(knockbackPower);

            m_PendingKnockbackPowers.Clear();
        }

        void Receive(HitEvent hitEvent)
        {
            HitResult result = hitEvent.Result;
            HitFeedbackContext feedback = hitEvent.Feedback;
            ShowDamageNumber(feedback.Point, result.Damage, result.Hurtbox.transform);
            m_HitVfx.Play(feedback);
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
