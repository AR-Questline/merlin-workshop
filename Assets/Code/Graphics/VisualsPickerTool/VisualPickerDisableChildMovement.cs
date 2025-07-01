#if UNITY_EDITOR
using System.Collections.Generic;
using Awaken.Utility.Extensions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Graphics.VisualsPickerTool {
    [InitializeOnLoad]
    public static class VisualPickerDisableChildMovement {
        static List<Transform> s_targets = new();
        static List<Transform> s_parents = new();

        static VisualPickerDisableChildMovement() {
            Selection.selectionChanged += OnSelectionChanged;
            EditorSceneManager.sceneSaving += (scene, path) => ApplyChanges();
            PrefabStage.prefabSaving += prefab => ApplyChanges();
            AssemblyReloadEvents.beforeAssemblyReload += ApplyChanges;
        }

        static void ApplyChanges() {
            bool anyChange = false;
            
            for (var index = 0; index < s_targets.Count; index++) {
                Transform target = s_targets[index];
                if (target == null) continue;
                Transform parent = s_parents[index];
                if (parent == null) continue;
                
                // Visual picker has to remain the parent
                if (target.parent != parent) {
                    if (!anyChange) {
                        Undo.IncrementCurrentGroup();
                        anyChange = true;
                    }
                    Undo.SetTransformParent(parent, target.parent, "Visuals Picker Transform Sync");
                    Undo.SetTransformParent(target, parent, "Visuals Picker Transform Sync");
                }
                
                // Visual has to remain at the origin of the parent
                if (target.localPosition != Vector3.zero || target.localRotation != Quaternion.identity || target.localScale != Vector3.one) {
                    if (!anyChange) {
                        Undo.IncrementCurrentGroup();
                        anyChange = true;
                    }
                    Undo.RecordObject(parent, "Visuals Picker Transform Sync");
                    Undo.RecordObject(target, "Visuals Picker Transform Sync");
                    parent.SetFromWorldSpaceMatrix(target.localToWorldMatrix);
                    target.localPosition = Vector3.zero;
                    target.localRotation = Quaternion.Euler(Vector3.zero);
                    target.localScale = Vector3.one;

                    EditorUtility.SetDirty(parent);
                    EditorUtility.SetDirty(target);
                }
            }
            if (anyChange) {
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
        }

        static void OnSelectionChanged() {
            ApplyChanges();
            s_targets.Clear();
            s_parents.Clear();
            if (Selection.transforms.Length > 0) {
                foreach (var transform in Selection.transforms) {
                    if (transform.parent is { } parent
                        && transform.parent.GetComponent<VisualsPicker>() is { } vp
                        && vp.CurrentVisuals == transform.gameObject) {
                        s_targets.Add(transform);
                        s_parents.Add(parent);
                    }
                }
            }
        }
    }
}
#endif