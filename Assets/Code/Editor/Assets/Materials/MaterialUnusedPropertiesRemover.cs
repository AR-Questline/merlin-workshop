using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Materials {
    public class MaterialUnusedPropertiesRemover : OdinEditorWindow {
        static readonly MethodInfo RemoveUnusedPropertiesMethod = typeof(Material).GetMethod("RemoveUnusedProperties", BindingFlags.NonPublic | BindingFlags.Instance);
        [SerializeField] List<Material> materials = new();
        
        [Button]
        void Gather(Shader shader) {
            materials.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Material")) {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.shader == shader) {
                    materials.Add(material);
                }
            }
        }

        [Button]
        void GatherAll() {
            materials.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Material")) {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                materials.Add(material);
            }
        }
        
        [Button]
        void Remove() {
            AssetDatabase.StartAssetEditing();
            try {
                foreach (var material in materials) {
                    RemoveUnusedPropertiesMethod.Invoke(material, null);
                    EditorUtility.SetDirty(material);
                }
            } finally {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("TG/Assets/Materials/Remove Unused Properties - Window")]
        static void Open() {
            GetWindow<MaterialUnusedPropertiesRemover>().Show();
        }

        [MenuItem("TG/Assets/Materials/Remove Unused Properties")]
        static void RemoveAll() {
            var window = GetWindow<MaterialUnusedPropertiesRemover>();
            window.GatherAll();
            window.Remove();
            window.Close();
        }
    }
}