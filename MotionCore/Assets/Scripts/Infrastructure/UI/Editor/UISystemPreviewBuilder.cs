#if UNITY_EDITOR
using System;
using MotionCore.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MotionCore.Editor
{
    /// <summary>
    /// 根据 UISystemConfig 在场景中重建 UIRoot 预览。
    /// </summary>
    public static class UISystemPreviewBuilder
    {
        /// <summary>
        /// 在当前场景重建配置对应的 UI 层级预览。
        /// </summary>
        public static RectTransform RebuildPreview(UISystemConfig config, RectTransform targetRoot)
        {
            RectTransform root = ResolveRoot(config, targetRoot);
            StretchRoot(root);
            DeleteGeneratedLayers(root);

            for (int i = 0; i < config.Layers.Count; i++)
            {
                UISystemConfig.LayerEntry layer = config.Layers[i];
                CreateLayer(root, config, layer);
            }

            EditorUtility.SetDirty(root.gameObject);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            return root;
        }

        static RectTransform ResolveRoot(UISystemConfig config, RectTransform targetRoot)
        {
            if (targetRoot != null)
            {
                return targetRoot;
            }

            GameObject existingRoot = GameObject.Find(config.Root.RootName);
            if (existingRoot != null)
            {
                RectTransform rect = existingRoot.GetComponent<RectTransform>();
                if (rect == null)
                    throw new InvalidOperationException("同名 UI 根节点缺少 RectTransform：" + config.Root.RootName);

                return rect;
            }

            var rootObject = new GameObject(config.Root.RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create UI Root Preview");
            return rootObject.GetComponent<RectTransform>();
        }

        static void DeleteGeneratedLayers(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (!child.name.StartsWith(UISystemConfig.DefaultLayerPrefix))
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        static void CreateLayer(RectTransform root, UISystemConfig config, UISystemConfig.LayerEntry layer)
        {
            string layerName = UISystemConfig.DefaultLayerPrefix + layer.LayerId;
            var layerObject = new GameObject(layerName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            if (layer.HasGraphicRaycaster)
            {
                layerObject.AddComponent<GraphicRaycaster>();
            }

            Undo.RegisterCreatedObjectUndo(layerObject, "Create UI Layer Preview");
            layerObject.transform.SetParent(root, false);

            RectTransform layerRoot = layerObject.GetComponent<RectTransform>();
            StretchRoot(layerRoot);

            Canvas canvas = layerObject.GetComponent<Canvas>();
            ConfigureCanvas(canvas, config, layer);

            CanvasScaler scaler = layerObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = config.Root.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = config.Root.MatchWidthOrHeight;
        }

        static void ConfigureCanvas(Canvas canvas, UISystemConfig config, UISystemConfig.LayerEntry layer)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = layer.SortingOrder;

            if (config.Root.RenderMode == UISystemConfig.UIRenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = ResolveCamera(config);
            canvas.planeDistance = config.Root.PlaneDistance;
        }

        static Camera ResolveCamera(UISystemConfig config)
        {
            if (config.Root.CameraResolveMode == UISystemConfig.UICameraResolveMode.MainCamera)
            {
                return Camera.main;
            }

            GameObject tagged = GameObject.FindWithTag(config.Root.CameraTag);
            return tagged ? tagged.GetComponent<Camera>() : null;
        }

        static void StretchRoot(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
        }
    }
}
#endif
