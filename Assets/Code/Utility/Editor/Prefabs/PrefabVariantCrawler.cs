using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.Prefabs {
    public static class PrefabVariantCrawler {
        public static Node<PrefabNodeData>[] Gather(Func<GameObject, bool> predicate, string[] folders) {
            var guids = AssetDatabase.FindAssets("t:Prefab", folders);
            var prefabs = new List<PrefabWithParent>();
            for (int i = 0; i < guids.Length; i++) {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!predicate(prefab)) {
                    continue;
                }
                TryGetParent(prefab, out var parent);
                prefabs.Add(new PrefabWithParent(prefab, parent, path));
            }
            return CollectChildren(prefabs.ToArray(), null);
        }
        
        public static Node<T1>[] Select<T0, T1>(Node<T0>[] source, Selector<T0, T1> selector) where T0 : INodeData where T1 : INodeData {
            return Select(null, source, selector);
        }
        
        static Node<T1>[] Select<T0, T1>(Transform parentPrefabTransform, Node<T0>[] variants, Selector<T0, T1> selector) where T0 : INodeData where T1 : INodeData {
            var datas = new Node<T1>[variants.Length];
            for (int i = 0; i < variants.Length; i++) {
                ref var node = ref variants[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(node.data.Path);
                try {
                    datas[i] = new Node<T1>(selector(parentPrefabTransform, node.data), Select(node.data.Prefab.transform, node.variants, selector));
                } catch (Exception e) {
                    Debug.LogError($"Error while processing {prefab}", prefab);
                    throw;
                }
            }
            return datas;
        }
        
        public static void Foreach<T>(Node<T>[] source, ForeachAction<T> action) where T : INodeData {
            var context = new ForeachContext();
            for (int i = 0; i < source.Length; i++) {
                ref var node = ref source[i];
                var editablePrefab = PrefabUtility.LoadPrefabContents(node.data.Path);
                bool success;
                try {
                    context.Reset();
                    action(node.data, editablePrefab, context);
                    success = true;
                } catch (Exception e) {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(node.data.Path);
                    Debug.LogError($"Error while processing {prefab}", prefab);
                    Debug.LogException(e, prefab);
                    context.save = false;
                    context.reimport = false;
                    success = false;
                }
                if (context.save) {
                    EditorUtility.SetDirty(editablePrefab);
                    PrefabUtility.SaveAsPrefabAsset(editablePrefab, node.data.Path);
                }
                PrefabUtility.UnloadPrefabContents(editablePrefab);
                if (context.reimport) {
                    AssetDatabase.ImportAsset(node.data.Path, ImportAssetOptions.ForceUpdate);
                }
                if (success) {
                    Foreach(node.variants, action);
                }
            }
        }

        public static bool TryGetParent(GameObject prefab, out GameObject prefabParent) {
            prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
            if (prefabParent == null || prefabParent == prefab) {
                prefabParent = null;
                return false;
            }
            return true;
        }
        
        public static bool TryGetInParent<TObject>(Transform parentPrefabTransform, TObject current, out TObject parent) where TObject : Component {
            if (parentPrefabTransform == null) {
                parent = null;
                return false;
            }
            parent = PrefabUtility.GetCorrespondingObjectFromSource(current);
            if (parent == null || parent == current || !parent.transform.IsChildOf(parentPrefabTransform)) {
                parent = null;
                return false;
            }
            return true;
        }

        public static TObject GetFromSource<TObject>(Transform parentPrefabTransform, TObject current, out Source source) where TObject : Component {
            if (parentPrefabTransform == null) {
                source = Source.CurrentPrefab;
                return current;
            }
            var parent = PrefabUtility.GetCorrespondingObjectFromSource(current);
            if (parent == null || parent == current) {
                source = Source.CurrentPrefab;
                return current;
            }
            if (parent.transform.IsChildOf(parentPrefabTransform)) {
                source = Source.ParentPrefab;
                return parent;
            }
            source = Source.NestedPrefab;
            return parent;
        }
        
        static Node<PrefabNodeData>[] CollectChildren(PrefabWithParent[] prefabs, GameObject parent) {
            var nodes = new List<Node<PrefabNodeData>>();
            for (int i = 0; i < prefabs.Length; i++) {
                ref readonly var prefab = ref prefabs[i];
                if (prefab.parent == parent) {
                    nodes.Add(new Node<PrefabNodeData>(new PrefabNodeData(prefab.path), CollectChildren(prefabs, prefab.prefab)));
                }
            }
            return nodes.ToArray();
        }

        public interface INodeData {
            string Path { get; }
            GameObject Prefab { get; set; }
        }
        
        [Serializable]
        public struct PrefabNodeData : INodeData {
            public string Path { get; }
            public GameObject Prefab {
                get => _prefab;
                set => _prefab = value;
            }

            [SerializeField] GameObject _prefab;

            public PrefabNodeData(string path) {
                Path = path;
                _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            }
        }
        
        [Serializable]
        public struct Node<TData> where TData : INodeData {
            public TData data;
            public Node<TData>[] variants;

            public Node(TData data, Node<TData>[] variants) {
                this.data = data;
                this.variants = variants;
            }
        }
        
        readonly struct PrefabWithParent {
            public readonly GameObject prefab;
            public readonly GameObject parent;
            public readonly string path;

            public PrefabWithParent(GameObject prefab, GameObject parent, string path) {
                this.prefab = prefab;
                this.parent = parent;
                this.path = path;
            }
        }

        public enum Source {
            /// <summary> Inherited from parent prefab </summary>
            ParentPrefab,
            /// <summary> Nested Prefab added in this prefab variant </summary>
            NestedPrefab,
            /// <summary> Plain GameObject added in this parent variant </summary>
            CurrentPrefab,
        }

        public delegate T1 Selector<in T0, out T1>(Transform parentPrefabTransform, T0 current) where T0 : INodeData where T1 : INodeData;
        public delegate void ForeachAction<in T>(T data, GameObject editablePrefab, ForeachContext context) where T : INodeData;

        public class ForeachContext {
            public bool save;
            public bool reimport;

            public void Reset() {
                save = false;
                reimport = false;
            }
        }
    }
}