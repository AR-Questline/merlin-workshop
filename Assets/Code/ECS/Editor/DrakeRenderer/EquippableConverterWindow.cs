using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public class EquippableConverterWindow : OdinEditorWindow {
        [ShowInInspector, AssetsOnly, AssetSelector]
        List<GameObject> _equippablesToConvert = new List<GameObject>();

        [Button]
        void Convert() {
            AssetDatabase.StartAssetEditing();
            try {
                foreach (var gameObject in _equippablesToConvert) {
                    Convert(gameObject);
                }
            } finally {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
        }

        void Convert(GameObject gameObject) {
            if (!gameObject) {
                return;
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(gameObject)) {
                return;
            }

            using var prefabEditing = new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(gameObject));
            var editablePrefab = prefabEditing.prefabContentsRoot;

            var rootTransform = editablePrefab.transform;
            for (int i = rootTransform.childCount - 1; i >= 0; --i) {
                var child = rootTransform.GetChild(i);
                TryMoveChildren(child, editablePrefab);
            }

            editablePrefab.isStatic = true;

            DrakePrefabConverter.ConvertEditableOPrefab(editablePrefab);
        }

        void TryMoveChildren(Transform current, GameObject editablePrefab) {
            current.gameObject.isStatic = true;
            for (int i = current.childCount - 1; i >= 0; --i) {
                TryMoveChildren(current.GetChild(i), current.gameObject);
            }
            if (CanMoveChildToRoot(current)) {
                MoveChildToRoot(current, editablePrefab);
            }
        }

        bool CanMoveChildToRoot(Transform child) {
            if (child.childCount != 0) {
                return false;
            }
            if (child.localScale != Vector3.one) {
                return false;
            }
            child.transform.GetLocalPositionAndRotation(out var position, out var rotation);
            if (position != Vector3.zero || rotation != Quaternion.identity) {
                return false;
            }
            return true;
        }

        void MoveChildToRoot(Transform child, GameObject editablePrefab) {
            bool movedAll = true;
            var components = child.GetComponents<Component>();
            foreach (var component in components) {
                if (component is Transform) {
                    continue;
                }
                var movedComponent = editablePrefab.AddComponent(component.GetType());
                if (!movedComponent) {
                    movedAll = false;
                    break;
                }
                EditorUtility.CopySerialized(component, movedComponent);
            }
            if (movedAll) {
                DestroyImmediate(child.gameObject);
            }
        }

        [MenuItem("TG/Assets/Drake/Convert equippable to drake")]
        static void ShowWindow() {
            var window = GetWindow<EquippableConverterWindow>();
            window.titleContent = new GUIContent("Convert equippable to drake");
            window.Show();
        }
    }
}
