using System;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Editor.Graphics {
    public class ShaderVariantCollectionWindow : OdinEditorWindow {
        [MenuItem("TG/Graphics/Shader Variant Collection window", false, 100)]
        static void OpenWindow() {
            GetWindow<ShaderVariantCollectionWindow>("Shader Variant Collection");
        }

        [ShowInInspector]
        public ShaderVariantCollection toInvestigate;

        [ShowInInspector]
        public int VariantsCount => toInvestigate != null ? toInvestigate.variantCount : 0;

        [Button]
        public void Join(ShaderVariantCollection[] collections) {
            if (collections == null || collections.Length == 0) {
                Debug.LogError("Please select at least one ShaderVariantCollection asset.");
                return;
            }

            var originalPath = AssetDatabase.GetAssetPath(collections[0]);
            var directory = Path.GetDirectoryName(originalPath);
            var filename = Path.GetFileNameWithoutExtension(originalPath);

            var newCollection = new ShaderVariantCollection { name = filename + "_Joined" };

            foreach (var collection in collections) {
                if (collection == null) {
                    Debug.LogWarning("Skipping null ShaderVariantCollection.");
                    continue;
                }

                var so = new SerializedObject(collection);
                var shadersProp = so.FindProperty("m_Shaders");
                var shaderCount = shadersProp.arraySize;

                for (var i = 0; i < shaderCount; i++) {
                    var pair = shadersProp.GetArrayElementAtIndex(i);
                    var shaderObj = pair.FindPropertyRelative("first").objectReferenceValue;
                    if (shaderObj == null) {
                        continue;
                    }
                    var shader = shaderObj as Shader;
                    var variantList = pair.FindPropertyRelative("second").FindPropertyRelative("variants");

                    for (int v = 0; v < variantList.arraySize; v++) {
                        var variant = variantList.GetArrayElementAtIndex(v);
                        var passType = (PassType)variant.FindPropertyRelative("passType").intValue;
                        var keywords = variant.FindPropertyRelative("keywords").stringValue.Split(' ');
                        try {
                            var entry = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);
                            newCollection.Add(entry);
                        } catch (Exception e) {
                            Debug.LogWarning($"Invalid variant in collection {collection.name} at index {i}, variant {v}: {e}");
                        }
                    }
                }
            }

            CreateCollectionAsset(newCollection, directory);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Joined {collections.Length} collections into '{newCollection.name}'.");
        }

        [Button]
        public void Split(ShaderVariantCollection collection, ushort countPerSplit = 250) {
            if (collection == null) {
                Debug.LogError("Please select a ShaderVariantCollection asset.");
                return;
            }

            var originalPath = AssetDatabase.GetAssetPath(collection);
            var directory = Path.GetDirectoryName(originalPath);
            var filename = Path.GetFileNameWithoutExtension(originalPath);

            var so = new SerializedObject(collection);
            var shadersProp = so.FindProperty("m_Shaders");
            var shaderCount = shadersProp.arraySize;
            if (shaderCount <= 2) {
                Debug.LogWarning("Not enough shader entries to split.");
                return;
            }

            var splitsCount = Mathf.CeilToInt((float)collection.variantCount / countPerSplit);

            // Create two new SVCs
            var splits = new ShaderVariantCollection[splitsCount];
            for (int s = 0; s < splitsCount; s++) {
                splits[s] = new ShaderVariantCollection { name = $"{filename}_Part{s + 1}" };
            }

            var variantIndex = 0;
            for (int i = 0; i < shaderCount; i++) {
                try {
                    var pair = shadersProp.GetArrayElementAtIndex(i);
                    var shaderObj = pair.FindPropertyRelative("first").objectReferenceValue;
                    if (shaderObj == null) {
                        continue;
                    }
                    var shader = shaderObj as Shader;
                    var variantList = pair.FindPropertyRelative("second").FindPropertyRelative("variants");

                    for (int v = 0; v < variantList.arraySize; v++) {
                        var splitsIndex = variantIndex++ / countPerSplit;
                        var partCollection = splits[splitsIndex];

                        var variant = variantList.GetArrayElementAtIndex(v);
                        var passType = (PassType)variant.FindPropertyRelative("passType").intValue;
                        var keywords = variant.FindPropertyRelative("keywords").stringValue.Split(' ');
                        var entry = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);
                        partCollection.Add(entry);
                    }
                } catch (Exception e) {
                    Debug.Log("Exception at index " + i);
                    Debug.LogException(e);
                }
            }

            for (int s = 0; s < splitsCount; s++) {
                CreateCollectionAsset(splits[s], directory);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Split '{filename}' into '{splitsCount} parts.");
        }

        static void CreateCollectionAsset(ShaderVariantCollection svc, string directory) {
            string path = Path.Combine(directory, svc.name + ".shadervariants");
            AssetDatabase.CreateAsset(svc, path);
        }
    }
}
