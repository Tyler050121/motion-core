using System;
using UnityEngine;

namespace MotionCore.Gameplay.Combat
{
    /// <summary>
    /// 管理死亡溶解的 Renderer 状态。
    /// </summary>
    sealed class DeathDissolveView
    {
        static readonly int DissolveThresholdId = Shader.PropertyToID("_DissolveThreshold");

        readonly MaterialPropertyBlock m_PropertyBlock = new();
        readonly RendererState[] m_States;

        public DeathDissolveView(Transform root)
        {
            Renderer[] renderers = Array.FindAll(
                root.GetComponentsInChildren<Renderer>(true),
                renderer => renderer is SkinnedMeshRenderer or MeshRenderer);
            m_States = Array.ConvertAll(renderers, renderer => new RendererState(renderer));

            if (m_States.Length == 0)
                throw new InvalidOperationException($"DeathDissolveView 未在 {root.name} 找到 Renderer。");

            foreach (RendererState state in m_States)
                state.ValidateMaterials(DissolveThresholdId);
        }

        /// <summary>
        /// 更新溶解进度。
        /// </summary>
        public void SetProgress(float progress)
        {
            foreach (RendererState state in m_States)
            {
                Renderer renderer = state.Renderer;
                renderer.GetPropertyBlock(m_PropertyBlock);
                m_PropertyBlock.SetFloat(DissolveThresholdId, progress);
                renderer.SetPropertyBlock(m_PropertyBlock);
            }
        }

        /// <summary>
        /// 设置死亡视觉的显示状态。
        /// </summary>
        public void SetVisible(bool visible)
        {
            foreach (RendererState state in m_States)
                state.SetVisible(visible);
        }

        /// <summary>
        /// 恢复溶解进度和 Renderer 状态。
        /// </summary>
        public void Restore()
        {
            SetProgress(0f);
            SetVisible(true);
        }

        readonly struct RendererState
        {
            readonly Renderer m_Renderer;
            readonly bool m_OriginalEnabled;

            public RendererState(Renderer renderer)
            {
                m_Renderer = renderer;
                m_OriginalEnabled = renderer.enabled;
            }

            public Renderer Renderer => m_Renderer;

            public void ValidateMaterials(int propertyId)
            {
                foreach (Material material in m_Renderer.sharedMaterials)
                    if (!material.HasProperty(propertyId))
                        throw new InvalidOperationException($"{m_Renderer.name} 的材质不支持溶解。");
            }

            public void SetVisible(bool visible) =>
                m_Renderer.enabled = visible && m_OriginalEnabled;
        }
    }
}
