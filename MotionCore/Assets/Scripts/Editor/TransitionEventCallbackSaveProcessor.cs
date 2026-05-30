#if UNITY_EDITOR
using Animancer;
using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MotionCore.Editor
{
    /// <summary>
    /// Save-time safety net for MotionCore TransitionAsset event callback rules.
    /// </summary>
    public sealed class TransitionEventCallbackSaveProcessor : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is TransitionAssetBase transitionAsset)
                    {
                        TransitionEventCallbackEditor.FixTransitionAsset(
                            transitionAsset,
                            false);
                    }
                }
            }

            return paths;
        }
    }
}
#endif
