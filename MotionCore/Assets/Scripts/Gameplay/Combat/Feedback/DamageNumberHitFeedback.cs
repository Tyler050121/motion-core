using DamageNumbersPro;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Hurtbox))]
    public sealed class DamageNumberHitFeedback : MonoBehaviour
    {
        [SerializeField, Tooltip("受击弹字预设")] DamageNumber m_DamageNumber;
        [SerializeField, Tooltip("弹字生成偏移")] Vector3 m_WorldOffset = new(0f, 1f, 0f);
        [SerializeField, Tooltip("弹字跟随受击目标")] bool m_FollowTarget = true;

        Hurtbox m_Hurtbox;

        void Awake()
        {
            m_Hurtbox = GetComponent<Hurtbox>();
        }

        void OnEnable()
        {
            m_Hurtbox.HitReceived += Show;
        }

        void OnDisable()
        {
            m_Hurtbox.HitReceived -= Show;
        }

        void Show(HitResult result)
        {
            DamageNumber popup = m_DamageNumber.Spawn(result.Point + m_WorldOffset, result.Damage);
            if (m_FollowTarget)
                popup.SetFollowedTarget(result.Hurtbox.transform);
        }
    }
}
