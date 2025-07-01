using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.Prefabs;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using static Awaken.Utility.Editor.Prefabs.PrefabVariantCrawler;

namespace Awaken.Kandra.Editor {
    public class KandraMeshRefresher : OdinEditorWindow {
        [FolderPath] public string[] folders = Array.Empty<string>();
        [SerializeField, ListDrawerSettings(ShowFoldout = false)] Node<PrefabNodeData>[] prefabs = Array.Empty<Node<PrefabNodeData>>();
        [SerializeField] string rootBoneName = "Hips";
        [SerializeField] bool forceEnable = true;
        
        [SerializeField, ListDrawerSettings(ShowFoldout = false), ShowIf(nameof(HasErrors))] List<Error> errors = new();

        bool HasErrors => errors.Count > 0;
        
        [Button]
        void Gather() {
            prefabs = PrefabVariantCrawler.Gather(go => go.GetComponentInChildren<KandraRenderer>() != null, folders);
            errors.Clear();
        }
        
        [Button]
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
            [HideLabel, HorizontalGroup] public GameObject prefab;
            [HideLabel, HorizontalGroup, DisplayAsString] public string message;
        }
    }
}