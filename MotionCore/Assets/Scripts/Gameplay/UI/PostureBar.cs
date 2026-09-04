using UnityEngine;

namespace MotionCore.Gameplay.UI
{
    /// <summary>
    /// 显示当前架势比例。
    /// </summary>
    public sealed class PostureBar : Bar
    {
        [SerializeField, Min(0.01f), Tooltip("架势恢复的表现时间")] float m_RecoveryDuration = 0.35f;

        float m_TargetValue = 1f;

        void Update()
        {
            if (Value >= m_TargetValue)
                return;

            SetValue(Mathf.MoveTowards(
                Value,
                m_TargetValue,
                Time.unscaledDeltaTime / m_RecoveryDuration));
        }

        /// <summary>
        /// 刷新架势填充比例。架势减少立即表现，架势恢复平滑追赶目标值。
        /// </summary>
        public void SetPosture(float currentPosture, float maxPosture)
        {
            float value = currentPosture / maxPosture;
            if (value <= Value)
                SetValue(value);

            m_TargetValue = value;
        }

        /// <summary>
        /// 绑定新角色时立即刷新架势填充，避免复用对象池实例的旧显示值。
        /// </summary>
        public void SetPostureImmediate(float currentPosture, float maxPosture)
        {
            float value = Mathf.Clamp01(currentPosture / Mathf.Max(1f, maxPosture));
            m_TargetValue = value;
            SetValue(value);
        }
    }
}
