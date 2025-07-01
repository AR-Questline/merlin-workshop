using System.Collections.Generic;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Materials {
    public class MaterialPropertyRenameFixer : OdinEditorWindow {
        [SerializeField] Shader shader;
        [SerializeField] string oldPropertyName;
        [SerializeField] string newPropertyName;
        
        [SerializeField] List<Material> materials = new();
        
        Material[] _allMaterials;

        protected override void OnEnable() {
            base.OnEnable();
            _allMaterials = ArrayUtils.Select(AssetDatabase.FindAssets("t:Material"), guid => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        [Button]
        void Gather() {
            materials.Clear();
            foreach (var material in _allMaterials) {
                if (material.shader == shader) {
                    materials.Add(material);
                }
            }
        }

        [Button, HorizontalGroup("Fix")]
        void FixTexture() {
            int oldId = Shader.PropertyToID(oldPropertyName);
            int newId = Shader.PropertyToID(newPropertyName);
            foreach (var material in materials) {
                material.SetTexture(newId, material.GetTexture(oldId));
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        
        [Button, HorizontalGroup("Fix")]
        void FixColor() {
            int oldId = Shader.PropertyToID(oldPropertyName);
            int newId = Shader.PropertyToID(newPropertyName);
            foreach (var material in materials) {
                material.SetColor(newId, material.GetColor(oldId));
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        
        [Button, HorizontalGroup("Fix")]
        void FixFloat() {
            int oldId = Shader.PropertyToID(oldPropertyName);
            int newId = Shader.PropertyToID(newPropertyName);
            foreach (var material in materials) {
                material.SetFloat(newId, material.GetFloat(oldId));
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        
        [Button, HorizontalGroup("Fix")]
        void FixVector() {
            int oldId = Shader.PropertyToID(oldPropertyName);
            int newId = Shader.PropertyToID(newPropertyName);
            foreach (var material in materials) {
                material.SetVector(newId, material.GetVector(oldId));
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TG/Assets/Materials/Rename Fixer")]
        static void Open() {
            GetWindow<MaterialPropertyRenameFixer>().Show();
        }
    }
}