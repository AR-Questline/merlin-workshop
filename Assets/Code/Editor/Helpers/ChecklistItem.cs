using System;
using System.Collections.Generic;
using UnityEditor;

using UnityEngine;

namespace Awaken.TG.Editor.Helpers {
    /// <summary>
    /// A helper class for easily creating in editor checklists.
    /// </summary>
    public class ChecklistItem<T>{
        // the name of the thing this item is about
        public string name;
        // what to show when the thing is missing
        public string missingText;
        // this function finds the thing (if it exists) starting from some root game object
        public Func<GameObject, T> findObjectFn;
        // this function creates the object if it doesn't exist (callback from an automatically created "+ Add xxx" button)
        public Func<GameObject, T> createObjectFn;
        // this function checks the thing and produces additional warnings that will be automatically displayed
        public Func<T, ICollection<string>> warningsFn;
        // this action is called to prepare component for edition
        public Action<T> prepareComponentForEditing;
        // whether an edit button should be shown if the thing already exists
        public bool skipEditButton;
        // only show the edit button when there are warnings
        public bool editOnlyWithWarnings;

        // === GUI

        /// <summary>
        /// Renders the GUI for this checklist item, automatically checking all the conditions,
        /// showing the relevant messages and edit/add buttons.
        /// </summary>
        /// <param name="root"></param>
        public void DisplayGUI(Component root) {
            // determine state
            T component = findObjectFn(root.gameObject);
            bool missing = component == null;
            string buttonText = missing ? $"+ Add {name}" : $"Edit {name}";
            // missing?
            if (missing) {
                EditorGUILayout.HelpBox(missingText, MessageType.Warning);
            }
            // additional warnings
            ICollection<string> warnings = component != null ? warningsFn?.Invoke(component) : null;
            if (warnings != null) {
                foreach (string warning in warnings) {
                    EditorGUILayout.HelpBox(warning, MessageType.Info);
                }
            }
            
            // do not show any button if not in prefab mode
            if ((UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot != root.gameObject)) {
                return;
            }

            // button to create or edit
            bool hadWarnings = warnings?.Count > 0;
            bool showAdd = missing && createObjectFn != null;
            bool showEdit = !missing && !skipEditButton && (!editOnlyWithWarnings || hadWarnings);
            bool buttonVisible = showAdd || showEdit;
            if (buttonVisible && GUILayout.Button(buttonText)) {
                if (missing) {
                    component = createObjectFn(root.gameObject);
                    EditorUtility.SetDirty(root);
                }
                if (typeof(Component).IsAssignableFrom(typeof(T))) {
                    GameObject target = ((Component)(object)component).gameObject;
                    prepareComponentForEditing?.Invoke(component);
                    Selection.activeGameObject = target;
                } else {
                    prepareComponentForEditing?.Invoke(component);
                }
                SceneView.FrameLastActiveSceneView();
            }

        }
    }
}