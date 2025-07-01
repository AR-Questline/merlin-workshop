using System;
using Awaken.TG.EditorOnly.WorkflowTools;
using Awaken.TG.Graphics.VisualsPickerTool;
using Awaken.TG.Main.Locations.Setup;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    public static class TrueSelectionBase {
        static readonly Type[] BaseTypes = {
            typeof(VisualsPicker),
#if !ADDRESSABLES_BUILD
            typeof(PrioritizeSelection),
#endif
            typeof(LocationSpec)
        };
        
        static bool SelectionChanged { get; set; }
        static bool IgnoreSelfCall { get; set; }
        
        [InitializeOnLoadMethod]
        static void Init() {
            SceneView.beforeSceneGui -= BeforeSceneGUI;
            SceneView.beforeSceneGui += BeforeSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        static void OnSelectionChanged() {
            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (IgnoreSelfCall || (focusedWindow != null && focusedWindow.titleContent.text.Equals("Scene") == false)) {
                IgnoreSelfCall = false;
                return;
            }
            SelectionChanged = true;
        }

        static void BeforeSceneGUI(SceneView obj) {
            if (SelectionChanged) {
                SelectionChanged = false;
                CheckSelection();
            }
        }

        static Transform PreviousTarget { get; set; }

        static void CheckSelection() {
            if (Selection.count != 1) return;

            var current = (Selection.objects[0] as GameObject)?.transform;
            if (current == null) {
                PreviousTarget = null;
                return;
            }

            if (PreviousTarget != null && current.IsChildOf(PreviousTarget)) {
                return;
            }

            // search for base types in selection or in parents
            while (current != null) {
                if (TryFindBaseType(current, out Component found)) {
                    IgnoreSelfCall = true;
                    Transform activeTransform = found.transform;
                    Selection.SetActiveObjectWithContext(activeTransform, PreviousTarget);
                    PreviousTarget = activeTransform;
                    return;
                }
                current = current.parent;
            }
        }

        static bool TryFindBaseType(Transform target, out Component found) {
            found = null;
            for (int i = 0; i < BaseTypes.Length; i++) {
                if (target.TryGetComponent(BaseTypes[i], out found)) {
                    return true;
                }
            }
            return false;
        }
    }
}
