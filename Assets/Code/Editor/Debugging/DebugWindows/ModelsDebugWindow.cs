using System;
using Awaken.TG.Debugging.ModelsDebugs;
using Awaken.TG.Debugging.ModelsDebugs.Inspectors;
using Awaken.TG.Editor.Assets;
using Awaken.TG.MVC;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.DebugWindows {
    public class ModelsDebugWindow : EditorWindow {
        ModelsDebug _modelsDebug;
        int _nextUpdate = 0;

        [MenuItem("TG/Debug/Models Debug %u")]
        public static void ShowWindow() {
            var window = GetWindow<ModelsDebugWindow>();
            window.titleContent = new GUIContent("Models Debug");
        }

        // === Enabling
        void OnEnable() {
            MethodItemInspector.nicifyModelsDebug = TGEditorPreferences.Instance.nicifyModelsDebugButtons;
            MethodItemInspector.classMethodSeparator = TGEditorPreferences.Instance.classMethodSeparator;
            _modelsDebug = new ModelsDebug();
            _modelsDebug.Init(false);
            Selection.selectionChanged += SelectionChanged;
        }

        void OnDisable() {
            Selection.selectionChanged -= SelectionChanged;
        }

        void Update() {
            --_nextUpdate;
            if (focusedWindow != this && _nextUpdate < 1) {
                Repaint();
                _nextUpdate = TGEditorPreferences.Instance.modelsDebugUpdateInterval;
            }
        }

        // === Update
        void OnGUI() {
            if (!Application.isPlaying) {
                EditorGUILayout.LabelField("Work only in play mode");
                return;
            }
            
            try {
                _modelsDebug.RefreshNavigation();
                _modelsDebug.Draw();
            } catch (Exception e) {
                Debug.LogException(e);
                EditorGUILayout.LabelField($"Encounter error: \n {e}", GUILayout.ExpandHeight(true));
                if (GUILayout.Button("Copy Error")) {
                    GUIUtility.systemCopyBuffer = $"{e.Message}\n{e.StackTrace}";
                }
                if (GUILayout.Button("Refresh")) {
                    OnEnable();
                }
            }
        }

        void SelectionChanged() {
            var isFocused = EditorWindow.GetWindow<ModelsDebugWindow>(false, null, false)?.hasFocus ?? false;
            
            if (!Application.isPlaying || !isFocused || Selection.activeTransform == null) {
                return;
            }

            var gameObject = Selection.activeGameObject;
            var view = gameObject.GetComponentInParent<IView>();
            if (view is { IsInitialized: true }) {
                _modelsDebug.SetSelectedId(view.GenericTarget.ID);
            }
        }
    }
}