using UnityEngine;

namespace MotionCore.Gameplay.Character
{
    /// <summary>
    /// 将动画根位移交给 CharacterController 处理。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterRootMotionMotor : MonoBehaviour
    {
        [SerializeField] Animator m_Animator;
        [SerializeField] CharacterController m_Controller;
        [SerializeField, Tooltip("应用动画根旋转")] bool m_ApplyRootRotation = true;

        void OnAnimatorMove()
        {
            m_Controller.Move(m_Animator.deltaPosition);

            if (m_ApplyRootRotation)
                m_Controller.transform.rotation *= m_Animator.deltaRotation;
        }
    }
}
