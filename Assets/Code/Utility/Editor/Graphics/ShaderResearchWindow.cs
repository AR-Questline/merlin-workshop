using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Utility.Editor.Graphics {
    public class ShaderResearchWindow : OdinEditorWindow {
        [MenuItem("TG/Graphics/Shaders/Shader research")]
        static void ShowWindow() {
            var window = GetWindow<ShaderResearchWindow>();
            window.titleContent = new GUIContent("Shader research");
            window.Show();
        }

        [ShowInInspector, OnValueChanged(nameof(OnNewShader))]
        Shader shader;

        [ShowInInspector] string[] keywords;
        [ShowInInspector] MaterialData[] materials;
        [ShowInInspector] Dictionary<Keywords, Material[]> materialsByKeywords;

        void OnNewShader() {
            keywords = Array.Empty<string>();
            materials = Array.Empty<MaterialData>();
            materialsByKeywords = new Dictionary<Keywords, Material[]>();

            if (shader == null) {
                return;
            }

            keywords = shader.keywordSpace.keywordNames;

            materials = AssetDatabase.FindAssets($"t:Material")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .Where(m => m.shader == shader)
                .Select(m => new MaterialData(m))
                .ToArray();


            materialsByKeywords = materials
                .GroupBy(m => new Keywords { keywords = m.keywords }, m => m.material)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        struct MaterialData {
            public Material material;
            public string[] keywords;

            public MaterialData(Material material) {
                this.material = material;
                this.keywords = material.enabledKeywords.Select(k => k.name).OrderBy(n => n).ToArray();
            }
        }

        struct Keywords : IEquatable<Keywords> {
            public string[] keywords;

            public bool Equals(Keywords other) {
                if (keywords.Length != other.keywords.Length) {
                    return false;
                }

                for (var i = 0; i < keywords.Length; i++) {
                    if (keywords[i] != other.keywords[i]) {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj) {
                return obj is Keywords other && Equals(other);
            }

            public override int GetHashCode() {
                if (keywords == null || keywords.Length == 0) {
                    return 0;
                }
                var hash = keywords.Length;
                foreach (var keyword in keywords) {
                    hash = hash * 31 + keyword.GetHashCode();
                }
                return hash;
            }
        }
    }
}