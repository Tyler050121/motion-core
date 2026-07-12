#if UNITY_EDITOR
using System.Collections.Generic;
using MotionCore.Infrastructure;
using UnityEditorInternal;
using UnityEngine;

namespace MotionCore.Editor
{
    /// <summary>
    /// UI 系统配置校验器。
    /// </summary>
    public static class UISystemConfigValidator
    {
        /// <summary>
        /// 校验运行时必需的 UI 配置关系。
        /// </summary>
        public static bool Validate(UISystemConfig config, List<string> errors, List<string> warnings)
        {
            errors.Clear();
            warnings.Clear();

            if (config == null)
            {
                errors.Add("缺少 UISystemConfig 资产。");
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.Root.RootName))
            {
                errors.Add("RootName 不能为空。");
            }

            if (string.IsNullOrWhiteSpace(config.PrefabSearchPath))
            {
                errors.Add("PrefabSearchPath 不能为空。");
            }

            if (config.Root.RenderMode == UISystemConfig.UIRenderMode.ScreenSpaceCamera)
            {
                ValidateCamera(config, errors, warnings);
            }

            if (config.Layers.Count == 0)
            {
                errors.Add("至少需要一个 Layer。");
            }

            var layerIds = new HashSet<string>();
            var sortingOrders = new HashSet<int>();
            var panelIds = new HashSet<string>();
            var widgetIds = new HashSet<string>();

            for (int i = 0; i < config.Layers.Count; i++)
            {
                UISystemConfig.LayerEntry layer = config.Layers[i];
                if (layer == null)
                {
                    errors.Add("存在空 Layer 条目。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(layer.LayerId))
                {
                    errors.Add("Layer " + (i + 1) + " 的 LayerId 不能为空。");
                    continue;
                }

                if (!layerIds.Add(layer.LayerId))
                {
                    errors.Add("LayerId 重复: " + layer.LayerId);
                }

                if (!sortingOrders.Add(layer.SortingOrder))
                {
                    warnings.Add("SortingOrder 重复: " + layer.SortingOrder);
                }
            }

            if (!config.ContainsLayer(UISystemConfig.DefaultLayerIds.Normal))
            {
                warnings.Add("当前配置没有 Normal 层。");
            }

            for (int i = 0; i < config.Panels.Count; i++)
            {
                UISystemConfig.UIPanelEntry panel = config.Panels[i];
                if (panel == null)
                {
                    errors.Add("存在空 Panel 条目。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(panel.Id))
                {
                    errors.Add("Panel ID 不能为空。");
                    continue;
                }

                if (!panelIds.Add(panel.Id))
                {
                    errors.Add("Panel ID 重复: " + panel.Id);
                }

                if (string.IsNullOrWhiteSpace(panel.AssetKey))
                {
                    errors.Add("Panel 资源地址为空: " + panel.Id);
                }

                if (!config.ContainsLayer(panel.LayerId))
                {
                    errors.Add("Panel 层不存在: " + panel.Id + " -> " + panel.LayerId);
                }
            }

            for (int i = 0; i < config.Widgets.Count; i++)
            {
                UISystemConfig.UIWidgetEntry widget = config.Widgets[i];
                if (widget == null)
                {
                    errors.Add("存在空 Widget 条目。");
                    continue;
                }

                if (widget.InitialCapacity < 0 || widget.MinCachedCount < 0)
                {
                    errors.Add("Widget " + widget.Id + " 的 PoolConfig 不能小于 0。");
                }

                if (string.IsNullOrWhiteSpace(widget.Id))
                {
                    errors.Add("Widget ID 不能为空。");
                    continue;
                }

                if (!widgetIds.Add(widget.Id))
                {
                    errors.Add("Widget ID 重复: " + widget.Id);
                }

                if (string.IsNullOrWhiteSpace(widget.AssetKey))
                {
                    errors.Add("Widget 资源地址为空: " + widget.Id);
                }

                if (!config.ContainsLayer(widget.LayerId))
                {
                    errors.Add("Widget 层不存在: " + widget.Id + " -> " + widget.LayerId);
                }
            }

            return errors.Count == 0;
        }

        static void ValidateCamera(UISystemConfig config, List<string> errors, List<string> warnings)
        {
            if (config.Root.CameraResolveMode == UISystemConfig.UICameraResolveMode.MainCamera)
            {
                if (Camera.main == null)
                {
                    warnings.Add("当前场景没有 MainCamera，预览时可能看不到 Screen Space Camera 结果。");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(config.Root.CameraTag))
            {
                errors.Add("CameraTag 不能为空。");
                return;
            }

            string[] tags = InternalEditorUtility.tags;
            bool foundTag = false;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == config.Root.CameraTag)
                {
                    foundTag = true;
                    break;
                }
            }

            if (!foundTag)
            {
                errors.Add("CameraTag 不存在: " + config.Root.CameraTag);
                return;
            }

            if (GameObject.FindWithTag(config.Root.CameraTag) == null)
            {
                warnings.Add("当前场景没有找到相机标签对象: " + config.Root.CameraTag);
            }
        }
    }
}
#endif
