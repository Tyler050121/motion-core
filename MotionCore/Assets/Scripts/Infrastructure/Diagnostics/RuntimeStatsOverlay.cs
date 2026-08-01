using UnityEngine;

namespace MotionCore.Infrastructure
{
    /// <summary>
    /// 在屏幕右上角显示运行时帧率信息。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RuntimeStatsOverlay : MonoBehaviour
    {
        const float UpdateInterval = 0.25f;
        const float Width = 260f;
        const float Height = 120f;
        const float Margin = 20f;

        readonly GUIContent m_Content = new();

        GUIStyle m_Style;
        float m_ElapsedTime;
        int m_FrameCount;

        void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
            return;
#endif

            m_Content.text = "FPS      --\nFrame  --.- ms\nScale   1.00";
        }

        void Update()
        {
            m_ElapsedTime += Time.unscaledDeltaTime;
            m_FrameCount++;
            if (m_ElapsedTime < UpdateInterval)
                return;

            float frameTime = m_ElapsedTime / m_FrameCount;
            float framesPerSecond = 1f / frameTime;
            m_Content.text = $"FPS   {framesPerSecond,6:0}\nFrame {frameTime * 1000f,6:0.0} ms\n" +
                             $"Scale {Time.timeScale,6:0.00}";
            m_ElapsedTime = 0f;
            m_FrameCount = 0;
        }

        void OnGUI()
        {
            m_Style ??= CreateStyle();
            Rect rect = new(Screen.width - Width - Margin, Margin, Width, Height);
            GUI.Box(rect, m_Content, m_Style);
        }

        static GUIStyle CreateStyle()
        {
            GUIStyle style = new(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 26,
                padding = new RectOffset(18, 18, 12, 12)
            };
            style.normal.textColor = Color.white;
            return style;
        }
    }
}
