using Awaken.Utility.Collections;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(KandraRenderer))]
    public class KandraRendererEditor : OdinEditor {
        KandraMeshEditor _kandraMeshEditor;
        bool _showBlendshapes;
        bool _showDebug;
        OnDemandCache<Material, UnityEditor.Editor> _materialEditors = new(CreateEditor);

        protected override void OnEnable() {
            base.OnEnable();
            _ = ((KandraRenderer)target).rendererData.EDITOR_Materials;
        }

        protected override void OnDisable() {
            base.OnDisable();
            foreach (var (material, editor) in _materialEditors) {
                DestroyImmediate(editor);
            }
            _materialEditors.Clear();
        }
        
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var renderer = (KandraRenderer) target;
            var rendererData = renderer.rendererData;
            if (rendererData.blendshapeWeights.IsCreated && rendererData.mesh.blendshapesNames.Length > 0) {
                _showBlendshapes = EditorGUILayout.Foldout(_showBlendshapes, "Blendshapes");
                if (_showBlendshapes) {
                    ++EditorGUI.indentLevel;
                    for (var i = 0u; i < rendererData.blendshapeWeights.Length; i++) {
                        var weight = rendererData.blendshapeWeights[i];
                        EditorGUI.BeginChangeCheck();
                        var newWeight = EditorGUILayout.Slider(rendererData.mesh.blendshapesNames[i], weight, 0, 1);
                        if (EditorGUI.EndChangeCheck()) {
                            rendererData.blendshapeWeights[i] = newWeight;
                            if (!Application.isPlaying && SceneView.lastActiveSceneView != null) {
                                SceneView.lastActiveSceneView.Repaint();
                            }
                        }
                    }

                    --EditorGUI.indentLevel;
                }
            }

            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug");
            if (_showDebug) {
                renderer.DrawDebugInfo();
            }

            var originalMaterials = renderer.rendererData.materials;
            foreach (var originalMaterial in originalMaterials) {
                var materialEditor = _materialEditors[originalMaterial];
                UnityEditor.Editor.DrawFoldoutInspector(originalMaterial, ref materialEditor);
                _materialEditors[originalMaterial] = materialEditor;
            }
        }
    }
}