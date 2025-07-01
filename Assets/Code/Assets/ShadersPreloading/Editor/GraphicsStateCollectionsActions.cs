#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Awaken.TG.Assets.ShadersPreloading.Editor {
    public static class GraphicsStateCollectionsActions {
        const string CombineActionMenuName = "Assets/Combine Shaders Traces";
        const string CreateShaderVariantCollectionMenuName = "Assets/Create Shader Variant Collection";

        static GraphicsStateCollection[] Selected;

        [MenuItem(CombineActionMenuName, true)]
        static bool IsCombineActionValid() {
            Selected = Selection.GetFiltered<GraphicsStateCollection>(SelectionMode.Assets);
            return Selected.Length > 1;
        }

        [MenuItem(CreateShaderVariantCollectionMenuName, true)]
        static bool IsCreateShaderVariantCollectionValid() {
            Selected = Selection.GetFiltered<GraphicsStateCollection>(SelectionMode.Assets);
            return Selected.Length > 0;
        }

        [MenuItem(CombineActionMenuName)]
        static void CombineShadersTraces() {
            int addedVariantCount = 0;
            int addedGfxStateCount = 0;
            int combinedCollectionCount = 0;

            GraphicsStateCollection result = Selection.activeObject as GraphicsStateCollection;

            if (result == null) {
                result = Selected[0];
            }

            string resultPath = AssetDatabase.GetAssetPath(result);

            for (int i = 0; i < Selected.Length; i++) {
                GraphicsStateCollection collection = Selected[i];

                if (collection == result) {
                    continue;
                }

                if (collection.runtimePlatform != result.runtimePlatform ||
                    collection.graphicsDeviceType != result.graphicsDeviceType) {
                    continue;
                }

                List<GraphicsStateCollection.ShaderVariant> variants = new();
                collection.GetVariants(variants);
                foreach (GraphicsStateCollection.ShaderVariant v in variants) {
                    Shader shader = v.shader;
                    PassIdentifier passId = v.passId;
                    LocalKeyword[] keywords = v.keywords;

                    if (result.AddVariant(shader, passId, keywords)) {
                        addedVariantCount++;
                    }

                    List<GraphicsStateCollection.GraphicsState> states = new();
                    collection.GetGraphicsStatesForVariant(v, states);
                    foreach (GraphicsStateCollection.GraphicsState s in states) {
                        if (result.AddGraphicsStateForVariant(shader, passId, keywords, s)) {
                            addedGfxStateCount++;
                        }
                    }
                }

                string path = AssetDatabase.GetAssetPath(collection);
                AssetDatabase.DeleteAsset(path);
                combinedCollectionCount++;
            }

            result.SaveToFile(resultPath);

            if (combinedCollectionCount != 0) {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Log.Debug?.Info($"Combined {combinedCollectionCount} {nameof(GraphicsStateCollection)}s into {resultPath}. Added {addedVariantCount} variants and {addedGfxStateCount} graphics states", result);
        }

        [MenuItem(CreateShaderVariantCollectionMenuName)]
        static void CreateShaderVariantCollection() {
            foreach (var graphicsStateCollection in Selected) {
                var shaderVariantCollection = ConvertToShaderVariantCollection(graphicsStateCollection);
                var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(graphicsStateCollection));
                SaveShaderVariantCollectionAsAsset(shaderVariantCollection, folder);
            }
        }

        static ShaderVariantCollection ConvertToShaderVariantCollection(GraphicsStateCollection graphicsStateCollection) {
            var shaderVariantCollection = new ShaderVariantCollection();
            shaderVariantCollection.name = graphicsStateCollection.name;
            var gscVariants = new List<GraphicsStateCollection.ShaderVariant>();
            graphicsStateCollection.GetVariants(gscVariants);

            foreach (var gscVariant in gscVariants) {
                var passType = PassIdentifierToPassType(gscVariant.shader, gscVariant.passId);

                var svcVariant = new ShaderVariantCollection.ShaderVariant(
                    gscVariant.shader,
                    passType,
                    gscVariant.keywords.Select(x => x.name).ToArray()
                );
                shaderVariantCollection.Add(svcVariant);
            }

            return shaderVariantCollection;
        }

        public static PassType PassIdentifierToPassType(Shader shader, PassIdentifier passId)
        {
            // 1. Pull the LightMode tag off the pass
            //    (subshader index is implicit in FindPassTagValue overload)
            var tagValue = shader.FindPassTagValue(
                (int)passId.PassIndex,
                new ShaderTagId("LightMode")
            );                                                           
            string lightModeName = tagValue.name;                       

            // 2. Try to parse that LightMode name into the PassType enum
            if (Enum.TryParse<PassType>(lightModeName, out var passType))
                return passType;

            // 3. If it doesn’t match one of the built-in values,
            //    fall back to the SRP catch-all
            return PassType.ScriptableRenderPipeline;
        }
        
        static void SaveShaderVariantCollectionAsAsset(ShaderVariantCollection shaderVariantCollection, string assetFolder) {
            var assetPath = Path.Combine(assetFolder, shaderVariantCollection.name + ".shadervariants");
            var existing = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(assetPath);
            if (existing != null) {
                EditorUtility.CopySerialized(shaderVariantCollection, existing);
                Log.Debug?.Info($"Updated existing ShaderVariantCollection at {assetPath}");
            } else {
                AssetDatabase.CreateAsset(shaderVariantCollection, assetPath);
                Log.Debug?.Info($"Created ShaderVariantCollection asset at {assetPath}");
            }
        }
    }
}

#endif