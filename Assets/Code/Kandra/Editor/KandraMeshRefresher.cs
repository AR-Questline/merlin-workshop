using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.Helpers;
using Awaken.Utility.Editor.Prefabs;
using UnityEditor;
using UnityEngine;
using static Awaken.Utility.Editor.Prefabs.PrefabVariantCrawler;

namespace Awaken.Kandra.Editor {
    public class KandraMeshRefresher : AREditorWindow {
        [DirectoryPath] public string[] folders = Array.Empty<string>();
        [SerializeField] Node<PrefabNodeData>[] prefabs = Array.Empty<Node<PrefabNodeData>>();
        [SerializeField] string rootBoneName = "Hips";
        [SerializeField] bool forceEnable = true;
        [SerializeField] List<Error> errors = new();

        Vector2 _errorsScroll;

        protected override void OnEnable() {
            base.OnEnable();

            AddButton("Gather", Gather, () => folders.Length > 0);
            AddButton("Refresh", Refresh, () => prefabs.Length > 0);

            AddCustomDrawer(nameof(errors), DrawErrors);
        }

        void DrawErrors(SerializedProperty errorsProp) {
            if (errorsProp.arraySize <= 0) {
                return;
            }

            _errorsScroll = EditorGUILayout.BeginScrollView(_errorsScroll);
            EditorGUILayout.LabelField("Errors", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            for (int i = 0; i < errorsProp.arraySize; i++) {
                var errorProp = errorsProp.GetArrayElementAtIndex(i);
                var prefabProp = errorProp.FindPropertyRelative("prefab");
                var messageProp = errorProp.FindPropertyRelative("message");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(prefabProp.objectReferenceValue, typeof(GameObject), false);
                EditorGUILayout.LabelField(messageProp.stringValue);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        void Gather() {
            prefabs = PrefabVariantCrawler.Gather(go => go.GetComponentInChildren<KandraRenderer>() != null, folders);
            errors.Clear();
        }

        void Refresh() {
            errors.Clear();
            PrefabVariantCrawler.Foreach(prefabs, (node, prefab, context) => {
                if (TryRefresh(prefab, out var message)) {
                    context.save = true;
                    context.reimport = true;
                } else {
                    errors.Add(new Error {
                        prefab = node.Prefab,
                        message = message
                    });
                }
            });
        }

        bool TryRefresh(GameObject prefab, out string message) {
            try {
                foreach (var rig in prefab.GetComponentsInChildren<KandraRig>()) {
                    var renderers = rig.GetComponentsInChildren<KandraRenderer>(forceEnable);
                    var replaces = ArrayUtils.Select(renderers, renderer => new KandraMeshReplacer.ReplaceData {
                        renderer = renderer,
                        mesh = renderer.rendererData.EDITOR_sourceMesh,
                        rootBoneName = rootBoneName
                    });
                    var success = KandraMeshReplacer.Replace(replaces, out message);
                    if (!success) {
                        return false;
                    }
                    foreach (var renderer in renderers) {
                        renderer.enabled = true;
                    }
                }
                message = null;
                return true;
            } catch (Exception e) {
                message = e.Message;
                return false;
            }
        }

        [MenuItem("TG/Assets/Kandra/Refresh Meshes")]
        static void Open() {
            GetWindow<KandraMeshRefresher>().Show();
        }
        
        [Serializable]
        struct Error {
            public GameObject prefab;
            public string message;
        }
    }
}