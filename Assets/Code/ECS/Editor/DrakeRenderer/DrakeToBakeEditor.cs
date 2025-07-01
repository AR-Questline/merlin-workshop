using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    [CustomEditor(typeof(DrakeToBake))]
    public class DrakeToBakeEditor : OdinEditor {
        public static bool IsOnValidTarget(DrakeToBake drakeToBake) {
            var drakeLodGroup = drakeToBake.GetComponent<DrakeLodGroup>();
            var drakeMeshRenderer = drakeToBake.GetComponent<DrakeMeshRenderer>();
            return !drakeLodGroup && !drakeMeshRenderer;
        }

        public override void OnInspectorGUI() {
            var drakeToBake = (DrakeToBake)target;
            var isEditable = PrefabsHelper.IsLowestEditablePrefabStage(drakeToBake);

            DrawInspector(drakeToBake, isEditable);
        }

        void DrawInspector(DrakeToBake drakeToBake, bool isEditable) {
            using var _ = new EditorGUI.DisabledGroupScope(!isEditable);
            DrawValidationAndOperations(drakeToBake);
        }

        void DrawValidationAndOperations(DrakeToBake drakeToBake) {
            var lodGroup = drakeToBake.GetComponent<LODGroup>();
            var meshRenderer = drakeToBake.GetComponent<MeshRenderer>();

            if (!IsOnValidTarget(drakeToBake)) {
                DestroyImmediate(this);
                return;
            }

            if (!lodGroup && !meshRenderer) {
                EditorGUILayout.HelpBox("There is no LODGroup nor MeshRenderer",
                    MessageType.Error);
            } else if (lodGroup) {
                if (GUILayout.Button("Bake")) {
                    var drakeLodGroup = drakeToBake.gameObject.AddComponent<DrakeLodGroup>();
                    if (DrakeEditorHelpers.Bake(drakeLodGroup, lodGroup)) {
                        DestroyImmediate(drakeToBake);
                        DrakeLodGroup.OnAddedDrakeLodGroup(drakeLodGroup);
                    } else {
                        DestroyImmediate(drakeLodGroup);
                    }
                }
            } else if (meshRenderer) {
                var parentLodGroup = meshRenderer.gameObject.GetComponentInParent<LODGroup>();
                if (parentLodGroup) {
                    var lods = parentLodGroup.GetLODs();
                    if (lods.Any(l => l.renderers.Contains(meshRenderer))) {
                        EditorGUILayout.HelpBox("DrakeToBake must be added to LodGroup, not to a renderer.", MessageType.Error);
                        if (GUILayout.Button("Remove DrakeToBake")) {
                            DestroyImmediate(drakeToBake);
                            return;
                        }
                        if (!parentLodGroup.GetComponent<DrakeToBake>() &&
                            GUILayout.Button("Move to parent LODGroup")) {
                            DestroyImmediate(drakeToBake);
                            parentLodGroup.gameObject.AddComponent<DrakeToBake>();
                            Selection.activeGameObject = parentLodGroup.gameObject;
                        }
                    }
                } else if (GUILayout.Button("Bake")) {
                    var drakeLodGroup = drakeToBake.gameObject.AddComponent<DrakeLodGroup>();
                    if (DrakeEditorHelpers.Bake(drakeLodGroup, meshRenderer)) {
                        DestroyImmediate(drakeToBake);
                        DrakeLodGroup.OnAddedDrakeLodGroup(drakeLodGroup);
                    } else {
                        DestroyImmediate(drakeLodGroup);
                    }
                }
            }
        }
    }
}
