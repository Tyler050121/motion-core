using DamageNumbersPro;
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

        void Awake()
        {
            m_Hurtbox = GetComponent<Hurtbox>();
            m_ReactionHandler = transform.parent.GetComponentInChildren<IHitReactionHandler>();
        }

        void OnEnable()
        {
            m_Hurtbox.HitReceived += Receive;
        }

        void OnDisable()
        {
            m_Hurtbox.HitReceived -= Receive;
        }

        void Receive(HitResult result)
        {
            ShowDamageNumber(result);
            m_ReactionHandler?.ReceiveHit(result.KnockbackPower);
        }

        void ShowDamageNumber(HitResult result)
        {
            DamageNumber popup = m_DamageNumber.Spawn(result.Point + m_WorldOffset, result.Damage);
            if (m_FollowTarget)
                popup.SetFollowedTarget(result.Hurtbox.transform);
        }
    }
}
