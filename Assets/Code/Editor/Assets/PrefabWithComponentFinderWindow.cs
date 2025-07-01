using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Assets {
    public class PrefabWithComponentFinderWindow : OdinEditorWindow {
        [ShowInInspector]
        Type[] _types = Array.Empty<Type>();

        [ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
        List<GameObject> _results = new();

        [MenuItem("TG/Assets/Prefab With Component Finder")]
        static void ShowWindow() {
            var window = GetWindow<PrefabWithComponentFinderWindow>();
            window.titleContent = new GUIContent("Prefab With Component Finder");
            window.Show();
        }

        [Button]
        void Find() {
            var prefabGuids = AssetDatabase.FindAssets("t:prefab");
            var count = prefabGuids.Length;
            try {
                for (int i = 0; i < count; i++) {
                    string prefabGuid = prefabGuids[i];
                    try {
                        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (EditorUtility.DisplayCancelableProgressBar("Searching", $"Searching", i / (float)count)) {
                            break;
                        }
                        if (Fulfills(prefab)) {
                            _results.Add(prefab);
                        }
                    } catch (Exception e) {
                        Log.Important?.Error($"For prefab with guid: {prefabGuid} can not bake drake");
                        Debug.LogException(e);
                    }
                }
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        bool Fulfills(GameObject prefab) {
            foreach (var type in _types) {
                if (prefab.GetComponentInChildren(type) == null) {
                    return false;
                }
            }
            return true;
        }
    }
}
