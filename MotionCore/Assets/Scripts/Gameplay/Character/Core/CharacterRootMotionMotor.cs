using MotionCore.Gameplay.Combat;
using MotionCore.Gameplay.Common;
using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 将动画根位移交给 CharacterController 处理，
    /// 并在攻击态按前探结果修正水平位移。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterRootMotionMotor : MonoBehaviour, IConfigReceiver<CharacterDefinition>
    {
        const int MaxAttackBlockers = 8;

        [SerializeField] Animator m_Animator;
        [SerializeField] CharacterController m_Controller;
        [SerializeField] Character m_Character;
        [SerializeField, Tooltip("攻击位移阻挡层")] LayerMask m_AttackBlockLayers = ~0;
        [SerializeField, Tooltip("攻击阻挡前探距离")] float m_AttackBlockProbeDistance = 0.55f;
        [SerializeField, Tooltip("应用动画根旋转")] bool m_ApplyRootRotation = true;

        readonly Collider[] m_AttackBlockers = new Collider[MaxAttackBlockers];
        float m_MoveSpeedScale = 1f;

        public void Initialize(CharacterDefinition definition)
        {
            m_MoveSpeedScale = definition.Motor.MoveSpeedScale;
        }

        void OnAnimatorMove()
        {
            Vector3 displacement = ResolveDisplacement(m_Animator.deltaPosition);

            // 仅移动态按系数缩放水平位移调整移动速度；保留竖直分量不影响重力，攻击/受击位移不缩放。
            if (m_Character.StateMachine.CurrentState.Type == CharacterStateType.Move)
            {
                displacement.x *= m_MoveSpeedScale;
                displacement.z *= m_MoveSpeedScale;
            }

            m_Controller.Move(displacement);

            if (m_ApplyRootRotation)
                m_Controller.transform.rotation *= m_Animator.deltaRotation;
        }

        /// <summary>
        /// 计算本帧最终位移。
        /// 非攻击态直接返回原始位移。
        /// </summary>
        Vector3 ResolveDisplacement(Vector3 displacement)
        {
            if (!IsAttackState())
                return displacement;

            Vector3 resolvedDisplacement = displacement;
            if (WillOverlapAttackBlocker(displacement))
            {
                resolvedDisplacement.x = 0f;
                resolvedDisplacement.z = 0f;
            }

            return resolvedDisplacement;
        }

        /// <summary>
        /// 预测前方是否会碰到敌方 Hurtbox。
        /// 命中时返回 true。
        /// </summary>
        bool WillOverlapAttackBlocker(Vector3 displacement)
        {
            Vector3 horizontal = new(displacement.x, 0f, displacement.z);
            if (horizontal.sqrMagnitude <= 0.000001f)
                return false;

            float probeDistance = Mathf.Max(horizontal.magnitude, m_AttackBlockProbeDistance);
            Vector3 probeOffset = horizontal.normalized * probeDistance;
            GetControllerCapsule(probeOffset, out Vector3 top, out Vector3 bottom, out float radius);
            int count = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                m_AttackBlockers,
                m_AttackBlockLayers,
                QueryTriggerInteraction.Collide);

            Transform ownerRoot = m_Controller.transform.root;
            for (int i = 0; i < count; i++)
            {
                Collider target = m_AttackBlockers[i];
                if (target.transform.root == ownerRoot)
                    continue;

                if (target.GetComponentInParent<Hurtbox>() != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 按偏移量计算 CharacterController 胶囊范围。
        /// 结果用于前探检测。
        /// </summary>
        void GetControllerCapsule(Vector3 offset, out Vector3 top, out Vector3 bottom, out float radius)
        {
            Transform controllerTransform = m_Controller.transform;
            radius = m_Controller.radius;
            float height = Mathf.Max(m_Controller.height, radius * 2f);
            Vector3 center = controllerTransform.TransformPoint(m_Controller.center) + offset;
            Vector3 verticalOffset = controllerTransform.up * (height * 0.5f - radius);
            top = center + verticalOffset;
            bottom = center - verticalOffset;
        }

        /// <summary>
        /// 判断当前是否处于攻击类状态。
        /// 只覆盖攻击相关状态。
        /// </summary>
        bool IsAttackState()
        {
            CharacterStateType state = m_Character.StateMachine.CurrentState.Type;
            return state == CharacterStateType.BasicAttack
                || state == CharacterStateType.HeavyAttack
                || state == CharacterStateType.Skill
                || state == CharacterStateType.Ultimate;
        }
    }
}
