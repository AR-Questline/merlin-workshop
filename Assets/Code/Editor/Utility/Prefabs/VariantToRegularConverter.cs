using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Prefabs {
    public class VariantToRegularConverter : OdinEditorWindow {
        public List<GameObject> variants = new List<GameObject>();

        [MenuItem("TG/Assets/Prefabs/Convert variant to regular")]
        static VariantToRegularConverter ShowWindow() {
            var window = GetWindow<VariantToRegularConverter>();
            window.titleContent = new GUIContent("Convert variant to regular");
            window.Show();
            return window;
        }

        [Button]
        public void Convert() {
            AssetDatabase.StartAssetEditing();
            try {
                foreach (var variant in variants) {
                    if (variant) {
                        PrefabUtil.VariantToRegularPrefab(variant);
                    }
                }
            } finally {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();

            variants.Clear();
        }

        [Button, ShowIf(nameof(FolderIsSelected))]
        public void FindVariantsInSelectedFolder() {
            if (Selection.activeObject is DefaultAsset folder) {
                var folderPath = AssetDatabase.GetAssetPath(folder);
                var prefabGuids = AssetDatabase.FindAssets("t:prefab", new[] { folderPath });
                var count = prefabGuids.Length;

                for (int i = 0; i < count; i++) {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab && PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant) {
                        variants.Add(prefab);
                    }
                }
            }
        }

        bool FolderIsSelected() {
            return Selection.activeObject is DefaultAsset;
        }

        // === Editor show
        [InitializeOnLoadMethod]
        static void InitHeader() {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnPostHeaderGUI;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        static void OnPostHeaderGUI(UnityEditor.Editor editor) {
            if (editor.targets.Length != 1) {
                return;
            }
            if (editor.target is GameObject prefabAsset) {
                if (!PrefabUtility.IsPartOfPrefabAsset(prefabAsset)) {
                    return;
                }
                if (PrefabUtility.GetPrefabAssetType(prefabAsset) != PrefabAssetType.Variant) {
                    return;
                }
                if (GUILayout.Button("Convert variant to regular")) {
                    PrefabUtil.VariantToRegularPrefab(prefabAsset);
                }
            } else if (editor.target is DefaultAsset) {
                if (GUILayout.Button("Convert variant to regular in folder")) {
                    ShowWindow().FindVariantsInSelectedFolder();
                }
            }
        }
    }
}
