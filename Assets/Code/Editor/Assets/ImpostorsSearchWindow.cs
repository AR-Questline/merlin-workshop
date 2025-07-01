using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Rendering;
using Awaken.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets {
    public class ImpostorsSearchWindow : OdinEditorWindow {
        [ShowInInspector]
        List<GameObject> _impostors = new();

        [Button]
        void Search() {
            _impostors.Clear();
            var guids = AssetDatabase.FindAssets("t:prefab");

            var impostorsType = GetImpostorsType();

            foreach (var guid in guids) {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (HasImpostor(go, impostorsType)) {
                    _impostors.Add(go);
                }
            }
        }

        static Type GetImpostorsType() {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "AmplifyImpostors");
            if (assembly == null) {
                return null;
            }
            return assembly.GetTypes().FirstOrDefault(t => t.Name == "AmplifyImpostor");
        }

        bool HasImpostor(GameObject go, Type impostorsType) {
            if (impostorsType != null) {
                var impostors = go.GetComponentsInChildren(impostorsType, true);
                if (impostors.Length > 0) {
                    return true;
                }
            }
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var meshRenderer in renderers) {
                if (meshRenderer.gameObject.layer == RenderLayers.Impostor) {
                    return true;
                }
                if (meshRenderer.name.Contains("Impostor")) {
                    return true;
                }
                var materials = meshRenderer.sharedMaterials;
                foreach (var material in materials) {
                    if (material && material.shader && material.shader.name.Contains("Impostor")) {
                        return true;
                    }
                }
            }
            return false;
        }

        [MenuItem("TG/Assets/Impostors search")]
        static void ShowWindow() {
            var window = GetWindow<ImpostorsSearchWindow>();
            window.titleContent = new GUIContent("Impostors search");
            window.Show();
        }
    }
}
