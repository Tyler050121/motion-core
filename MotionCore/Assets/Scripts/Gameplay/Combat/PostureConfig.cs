using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 角色架势数值与恢复配置。
    /// </summary>
    [System.Serializable]
    public sealed class PostureConfig
    {
        [SerializeField, Min(1f), Tooltip("最大架势值")] float m_MaxPosture = 100f;
        [SerializeField, Min(0f), Tooltip("受击后恢复延迟")] float m_RecoveryDelay = 3f;
        [SerializeField, Min(0f), Tooltip("每秒恢复架势值")] float m_RecoveryPerSecond = 10f;

        public float MaxPosture => m_MaxPosture;
        public float RecoveryDelay => m_RecoveryDelay;
        public float RecoveryPerSecond => m_RecoveryPerSecond;
    }
}
