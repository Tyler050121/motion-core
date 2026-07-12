using UnityEngine;
using UnityEngine.UI;

namespace MotionCore.Gameplay.UI
{
    [DisallowMultipleComponent]
    public class Bar : MonoBehaviour
    {
        [SerializeField, Tooltip("填充图形，宽度决定最大长度")] Image m_Fill;
        [SerializeField, Tooltip("填充颜色渐变")] Gradient m_ColorGradient;

        float m_Value = 1f;
        float m_FillWidth;

        /// <summary>
        /// 设置填充比例和颜色。
        /// </summary>
        public void SetValue(float value)
        {
            value = Mathf.Clamp01(value);
            m_Value = value;
            ResizeBar(m_Fill.rectTransform, value);
            m_Fill.color = m_ColorGradient.Evaluate(value);
        }

        protected float Value => m_Value;

        protected void ResizeBar(RectTransform rect, float value)
        {
            if (m_FillWidth <= 0f)
                m_FillWidth = m_Fill.rectTransform.rect.width;

            Vector2 sizeDelta = rect.sizeDelta;
            Vector2 anchoredPosition = rect.anchoredPosition;

            anchoredPosition.x = -m_FillWidth * 0.5f;
            sizeDelta.x = m_FillWidth * Mathf.Clamp01(value);

            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
