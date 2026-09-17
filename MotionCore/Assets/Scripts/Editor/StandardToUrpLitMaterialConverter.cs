#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Converts legacy built-in Standard materials to URP Lit while preserving common material inputs.
/// </summary>
public static class StandardToUrpLitMaterialConverter
{
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    private const string StandardShaderName = "Standard";
    private const string StandardSpecularShaderName = "Standard (Specular setup)";

    [MenuItem("MotionCore/Rendering/Convert Selected Standard Materials To URP Lit")]
    private static void ConvertSelectedMaterials()
    {
        var materials = CollectMaterialsFromSelection();
        ConvertMaterials(materials, "selected assets and scene objects");
    }

    [MenuItem("MotionCore/Rendering/Convert All Standard Materials In Assets To URP Lit")]
    private static void ConvertAllProjectMaterials()
    {
        var materials = new HashSet<Material>();
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                materials.Add(material);
            }
        }

        ConvertMaterials(materials, "all project material assets");
    }

    [MenuItem("MotionCore/Rendering/Convert Open Scenes Standard Materials To URP Lit")]
    private static void ConvertOpenSceneMaterials()
    {
        var materials = new HashSet<Material>();
        foreach (var renderer in Object.FindObjectsByType<Renderer>())
        {
            AddRendererMaterials(renderer, materials);
        }

        ConvertMaterials(materials, "open scenes");
        EditorSceneManager.MarkAllScenesDirty();
    }

    private static HashSet<Material> CollectMaterialsFromSelection()
    {
        var materials = new HashSet<Material>();
        var selectedObjects = Selection.objects;

        foreach (var selectedObject in selectedObjects)
        {
            if (selectedObject is Material material)
            {
                materials.Add(material);
                continue;
            }

            if (selectedObject is GameObject gameObject)
            {
                AddGameObjectMaterials(gameObject, materials);
                continue;
            }

            var path = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (AssetDatabase.IsValidFolder(path))
            {
                AddMaterialsUnderFolder(path, materials);
                continue;
            }

            var assetMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (assetMaterial != null)
            {
                materials.Add(assetMaterial);
            }
        }

        return materials;
    }

    private static void AddMaterialsUnderFolder(string folderPath, ISet<Material> materials)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folderPath }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material != null)
            {
                materials.Add(material);
            }
        }
    }

    private static void AddGameObjectMaterials(GameObject gameObject, ISet<Material> materials)
    {
        foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>(true))
        {
            AddRendererMaterials(renderer, materials);
        }
    }

    private static void AddRendererMaterials(Renderer renderer, ISet<Material> materials)
    {
        foreach (var material in renderer.sharedMaterials)
        {
            if (material != null)
            {
                materials.Add(material);
            }
        }
    }

    private static void ConvertMaterials(IEnumerable<Material> materials, string scopeLabel)
    {
        var urpLitShader = Shader.Find(UrpLitShaderName);
        if (urpLitShader == null)
        {
            EditorUtility.DisplayDialog(
                "URP Lit shader not found",
                $"Could not find shader '{UrpLitShaderName}'. Make sure Universal Render Pipeline is installed.",
                "OK");
            return;
        }

        var convertedCount = 0;
        var skippedCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var material in materials)
            {
                if (material == null || material.shader == null || !IsBuiltInStandard(material.shader.name))
                {
                    skippedCount++;
                    continue;
                }

                Undo.RecordObject(material, "Convert Standard Material To URP Lit");
                ConvertMaterial(material, urpLitShader);
                EditorUtility.SetDirty(material);
                convertedCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Converted {convertedCount} Standard material(s) to URP Lit from {scopeLabel}. Skipped {skippedCount} material(s).");
        EditorUtility.DisplayDialog(
            "Standard To URP Lit",
            $"Converted {convertedCount} Standard material(s) to URP Lit.\nSkipped {skippedCount} material(s).",
            "OK");
    }

    private static bool IsBuiltInStandard(string shaderName)
    {
        return shaderName == StandardShaderName || shaderName == StandardSpecularShaderName;
    }

    private static void ConvertMaterial(Material material, Shader urpLitShader)
    {
        var source = CaptureStandardProperties(material);
        material.shader = urpLitShader;

        SetTexture(material, "_BaseMap", source.MainTexture, source.MainTextureScale, source.MainTextureOffset);
        SetColor(material, "_BaseColor", source.Color);
        SetFloat(material, "_Cutoff", source.Cutoff);

        if (source.UseSpecularWorkflow)
        {
            SetFloat(material, "_WorkflowMode", 0f);
            SetTexture(material, "_SpecGlossMap", source.SpecGlossMap, source.SpecGlossMapScale, source.SpecGlossMapOffset);
            SetColor(material, "_SpecColor", source.SpecColor);
        }
        else
        {
            SetFloat(material, "_WorkflowMode", 1f);
            SetTexture(material, "_MetallicGlossMap", source.MetallicGlossMap, source.MetallicGlossMapScale, source.MetallicGlossMapOffset);
            SetFloat(material, "_Metallic", source.Metallic);
        }

        SetFloat(material, "_Smoothness", source.Smoothness);
        SetTexture(material, "_BumpMap", source.NormalMap, source.NormalMapScale, source.NormalMapOffset);
        SetFloat(material, "_BumpScale", source.NormalScale);
        SetTexture(material, "_OcclusionMap", source.OcclusionMap, source.OcclusionMapScale, source.OcclusionMapOffset);
        SetFloat(material, "_OcclusionStrength", source.OcclusionStrength);
        SetTexture(material, "_EmissionMap", source.EmissionMap, source.EmissionMapScale, source.EmissionMapOffset);
        SetColor(material, "_EmissionColor", source.EmissionColor);

        ApplySurfaceMode(material, source.RenderMode, source.Color.a);
        ApplyUrpKeywords(material, source);
    }

    private static StandardMaterialProperties CaptureStandardProperties(Material material)
    {
        var shaderName = material.shader.name;

        return new StandardMaterialProperties
        {
            UseSpecularWorkflow = shaderName == StandardSpecularShaderName,
            RenderMode = GetFloat(material, "_Mode", 0f),
            Color = GetColor(material, "_Color", Color.white),
            MainTexture = GetTexture(material, "_MainTex"),
            MainTextureScale = GetTextureScale(material, "_MainTex"),
            MainTextureOffset = GetTextureOffset(material, "_MainTex"),
            Cutoff = GetFloat(material, "_Cutoff", 0.5f),
            Metallic = GetFloat(material, "_Metallic", 0f),
            MetallicGlossMap = GetTexture(material, "_MetallicGlossMap"),
            MetallicGlossMapScale = GetTextureScale(material, "_MetallicGlossMap"),
            MetallicGlossMapOffset = GetTextureOffset(material, "_MetallicGlossMap"),
            SpecColor = GetColor(material, "_SpecColor", Color.gray),
            SpecGlossMap = GetTexture(material, "_SpecGlossMap"),
            SpecGlossMapScale = GetTextureScale(material, "_SpecGlossMap"),
            SpecGlossMapOffset = GetTextureOffset(material, "_SpecGlossMap"),
            Smoothness = GetFloat(material, "_Glossiness", 0.5f),
            NormalMap = GetTexture(material, "_BumpMap"),
            NormalMapScale = GetTextureScale(material, "_BumpMap"),
            NormalMapOffset = GetTextureOffset(material, "_BumpMap"),
            NormalScale = GetFloat(material, "_BumpScale", 1f),
            OcclusionMap = GetTexture(material, "_OcclusionMap"),
            OcclusionMapScale = GetTextureScale(material, "_OcclusionMap"),
            OcclusionMapOffset = GetTextureOffset(material, "_OcclusionMap"),
            OcclusionStrength = GetFloat(material, "_OcclusionStrength", 1f),
            EmissionMap = GetTexture(material, "_EmissionMap"),
            EmissionMapScale = GetTextureScale(material, "_EmissionMap"),
            EmissionMapOffset = GetTextureOffset(material, "_EmissionMap"),
            EmissionColor = GetColor(material, "_EmissionColor", Color.black)
        };
    }

    private static void ApplySurfaceMode(Material material, float standardMode, float alpha)
    {
        var mode = Mathf.RoundToInt(standardMode);
        var transparent = mode == 2 || mode == 3 || alpha < 1f;
        var alphaClip = mode == 1;

        SetFloat(material, "_Surface", transparent ? 1f : 0f);
        SetFloat(material, "_AlphaClip", alphaClip ? 1f : 0f);
        SetFloat(material, "_ZWrite", transparent ? 0f : 1f);
        SetFloat(material, "_Cull", (float)CullMode.Back);

        if (!transparent)
        {
            SetFloat(material, "_Blend", 0f);
            SetFloat(material, "_SrcBlend", (float)BlendMode.One);
            SetFloat(material, "_DstBlend", (float)BlendMode.Zero);
            material.renderQueue = alphaClip ? (int)RenderQueue.AlphaTest : -1;
            return;
        }

        SetFloat(material, "_Blend", mode == 3 ? 1f : 0f);
        SetFloat(material, "_SrcBlend", mode == 3 ? (float)BlendMode.One : (float)BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", mode == 3 ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.OneMinusSrcAlpha);
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void ApplyUrpKeywords(Material material, StandardMaterialProperties source)
    {
        SetKeyword(material, "_ALPHATEST_ON", Mathf.RoundToInt(source.RenderMode) == 1);
        SetKeyword(material, "_NORMALMAP", source.NormalMap != null);
        SetKeyword(material, "_OCCLUSIONMAP", source.OcclusionMap != null);
        SetKeyword(material, "_EMISSION", source.EmissionMap != null || source.EmissionColor.maxColorComponent > 0f);
        SetKeyword(material, "_METALLICSPECGLOSSMAP", !source.UseSpecularWorkflow && source.MetallicGlossMap != null);
        SetKeyword(material, "_SPECGLOSSMAP", source.UseSpecularWorkflow && source.SpecGlossMap != null);
        SetKeyword(material, "_SPECULAR_SETUP", source.UseSpecularWorkflow);
        SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", Mathf.RoundToInt(source.RenderMode) == 2 || Mathf.RoundToInt(source.RenderMode) == 3 || source.Color.a < 1f);
    }

    private static Texture GetTexture(Material material, string propertyName)
    {
        return material.HasProperty(propertyName) ? material.GetTexture(propertyName) : null;
    }

    private static Vector2 GetTextureScale(Material material, string propertyName)
    {
        return material.HasProperty(propertyName) ? material.GetTextureScale(propertyName) : Vector2.one;
    }

    private static Vector2 GetTextureOffset(Material material, string propertyName)
    {
        return material.HasProperty(propertyName) ? material.GetTextureOffset(propertyName) : Vector2.zero;
    }

    private static float GetFloat(Material material, string propertyName, float fallback)
    {
        return material.HasProperty(propertyName) ? material.GetFloat(propertyName) : fallback;
    }

    private static Color GetColor(Material material, string propertyName, Color fallback)
    {
        return material.HasProperty(propertyName) ? material.GetColor(propertyName) : fallback;
    }

    private static void SetTexture(Material material, string propertyName, Texture texture, Vector2 scale, Vector2 offset)
    {
        if (!material.HasProperty(propertyName))
        {
            return;
        }

        material.SetTexture(propertyName, texture);
        material.SetTextureScale(propertyName, scale);
        material.SetTextureOffset(propertyName, offset);
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetColor(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    private struct StandardMaterialProperties
    {
        public bool UseSpecularWorkflow;
        public float RenderMode;
        public Color Color;
        public Texture MainTexture;
        public Vector2 MainTextureScale;
        public Vector2 MainTextureOffset;
        public float Cutoff;
        public float Metallic;
        public Texture MetallicGlossMap;
        public Vector2 MetallicGlossMapScale;
        public Vector2 MetallicGlossMapOffset;
        public Color SpecColor;
        public Texture SpecGlossMap;
        public Vector2 SpecGlossMapScale;
        public Vector2 SpecGlossMapOffset;
        public float Smoothness;
        public Texture NormalMap;
        public Vector2 NormalMapScale;
        public Vector2 NormalMapOffset;
        public float NormalScale;
        public Texture OcclusionMap;
        public Vector2 OcclusionMapScale;
        public Vector2 OcclusionMapOffset;
        public float OcclusionStrength;
        public Texture EmissionMap;
        public Vector2 EmissionMapScale;
        public Vector2 EmissionMapOffset;
        public Color EmissionColor;
    }
}
#endif
