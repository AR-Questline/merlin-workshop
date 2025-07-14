using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.Helpers;
using Awaken.Utility.Editor.Prefabs;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    public class BatchKandraCreator : AREditorWindow {
        [DirectoryPath] public string[] folders = Array.Empty<string>();
        public PrefabVariantCrawler.Node<PrefabVariantCrawler.PrefabNodeData>[] prefabs = Array.Empty<PrefabVariantCrawler.Node<PrefabVariantCrawler.PrefabNodeData>>();

        SerializedObject _serializedObject;
        bool _hasMissingScripts;

        [MenuItem("TG/Assets/Kandra/Batch Create")]
        public static void ShowWindow() {
            EditorWindow.GetWindow<BatchKandraCreator>().Show();
        }

        protected override void OnEnable() {
            base.OnEnable();

            AddButton("Gather Prefabs", Gather, HasValidFolders);
            AddButton("Remove Missing Scripts", RemoveMissingScripts, () => prefabs.Length > 0 && _hasMissingScripts);
            AddButton("Create Kandra", Create, () => prefabs.Length > 0 && !_hasMissingScripts);
        }

        protected override void OnGUI() {
            if (HasValidFolders() == false) {
                EditorGUILayout.HelpBox("Please choose at least one valid directory", MessageType.Info);
            }

            base.OnGUI();
        }

        void Gather() {
            prefabs = PrefabVariantCrawler.Gather(ShouldBeBaked, folders);
            if (prefabs.Length > 0) {
                _hasMissingScripts = HasMissingScripts();
            }
        }

        void RemoveMissingScripts() {
            PrefabVariantCrawler.Foreach(prefabs, (data, prefab, context) => {
                RemoveMissingScripts(prefab.transform);
                context.save = true;
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssetIfDirty(prefab);
            });
            _hasMissingScripts = false;
            
            static void RemoveMissingScripts(Transform transform) {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                foreach (Transform child in transform) {
                    RemoveMissingScripts(child);
                }
            }
        }

        void Create() {
            var creator = GetWindow<CreateKandra>();
            creator.StartBatchCreating();

            var data = PrefabVariantCrawler.Select(prefabs, (parentPrefabTransform, currentPrefab) => {
                var components = currentPrefab.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var ownedList = new List<OwningPrefabData>();
                var overridenList = new List<OverridenPrefabData>();
                var hasNestedPrefabs = false;

                foreach (var current in components) {
                    var parent = PrefabVariantCrawler.GetFromSource(parentPrefabTransform, current, out var source);
                    if (source is PrefabVariantCrawler.Source.CurrentPrefab) {
                        ownedList.Add(OwningPrefabData.Create(current));
                    } else if (source == PrefabVariantCrawler.Source.ParentPrefab) {
                        overridenList.Add(OverridenPrefabData.Create(parent, current));
                    } else if (source == PrefabVariantCrawler.Source.NestedPrefab) {
                        hasNestedPrefabs = true;
                    }
                }
                
                return new KandraData(currentPrefab, hasNestedPrefabs, ownedList.ToArray(), overridenList.ToArray());
            });
            
            PrefabVariantCrawler.Foreach(data, (data, prefab, context) => {
                if (data.owned.Length > 0 & data.overriden.Length > 0) {
                    throw new Exception($"Prefab {prefab} has both owned and overriden components. This is not supported.");
                }
                if (data.hasNestedPrefabs) {
                    throw new Exception($"Prefab {prefab} has nested prefabs. This is not supported.");
                }
                if (data.owned.Length > 0) {
                    Array.Sort(data.owned, (lhs, rhs) => lhs.Depth.CompareTo(rhs.Depth));
                    foreach (ref var owned in data.owned.RefIterator()) {
                        var smr = owned.path.Retrieve(prefab.transform).GetComponent<SkinnedMeshRenderer>();
                        if (!smr) { // mey be destroyed while processing previous smr
                            continue;
                        }
                        var animator = smr.GetComponentInParent<Animator>(true);
                        var root = animator ? animator.gameObject : smr.transform.parent.gameObject;
                        creator.ProcessSingleTarget(root);
                    }
                    context.save = true;
                    EditorUtility.SetDirty(prefab);
                    return;
                }
                bool dirty = false;
                foreach (var overriden in data.overriden) {
                    var gameObject = overriden.path.Retrieve(prefab.transform).gameObject;
                    var renderer = gameObject.GetComponent<KandraRenderer>();
                    var previousEnabled = renderer.enabled;
                    renderer.enabled = false;
                    bool changed = false;
                    if (overriden.mesh != null) {
                        renderer.rendererData.mesh = KandraMeshBaker.Create(overriden.mesh, overriden.rootBone, out _);
                        renderer.rendererData.EDITOR_sourceMesh = overriden.mesh;
                        changed = true;
                    }
                    if (overriden.materials != null) {
                        foreach (var material in overriden.materials) {
                            if (material != null) {
                                CreateKandra.TryRedirectShader(material);
                            }
                        }
                        renderer.EDITOR_ClearMaterials();
                        renderer.rendererData.materials = overriden.materials;
                        renderer.EDITOR_RecreateMaterials();
                        changed = true;
                    }
                    if (overriden.blendshapes != null) {
                        var blendshapes = gameObject.GetComponent<ConstantKandraBlendshapes>();
                        if (blendshapes == null) {
                            blendshapes = gameObject.AddComponent<ConstantKandraBlendshapes>();
                            renderer.rendererData.constantBlendshapes = blendshapes;
                        }
                        blendshapes.blendshapes = overriden.blendshapes;
                        changed = true;
                    }
                    renderer.enabled = previousEnabled;
                    if (changed) {
                        renderer.EDITOR_RenderingDataChanged();
                        dirty = true;
                    }
                }
                if (dirty) {
                    context.save = true;
                    EditorUtility.SetDirty(prefab);
                }
            });
            
            creator.FinishBatchCreating();
            creator.Close();
        }

        static bool ShouldBeBaked(GameObject prefab) {
            return prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null && prefab.GetComponentInChildren<KandraRenderer>(true) == null;
        }

        bool HasMissingScripts() {
            var hasAnyMissingScripts = false;

            PrefabVariantCrawler.Foreach(prefabs, (data, prefab, context) => {
                hasAnyMissingScripts = hasAnyMissingScripts || HasAnyMissingScripts(prefab.transform);
            });

            return hasAnyMissingScripts;

            static bool HasAnyMissingScripts(Transform transform) {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) {
                    return true;
                }
                foreach (Transform child in transform) {
                    if (HasAnyMissingScripts(child)) {
                        return true;
                    }
                }

                return false;
            }
        }

        bool HasValidFolders() {
            foreach (var folder in folders) {
                if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder)) {
                    return true;
                }
            }
            return false;
        }

        [Serializable]
        struct KandraData : PrefabVariantCrawler.INodeData {
            public string Path { get; }
            public GameObject Prefab { get; set; }
            
            public bool hasNestedPrefabs;
            public OwningPrefabData[] owned;
            public OverridenPrefabData[] overriden;
            
            public KandraData(in PrefabVariantCrawler.PrefabNodeData prefab, bool hasNestedPrefabs, OwningPrefabData[] owned, OverridenPrefabData[] overriden) {
                Path = prefab.Path;
                Prefab = prefab.Prefab;
                this.hasNestedPrefabs = hasNestedPrefabs;
                this.owned = owned;
                this.overriden = overriden;
            }
        }

        [Serializable]
        struct OwningPrefabData {
            public HierarchyPath path;
            public int Depth => path.names.Length;

            OwningPrefabData(SkinnedMeshRenderer renderer) {
                path = new HierarchyPath(renderer.transform);
            }

            public static OwningPrefabData Create(SkinnedMeshRenderer current) {
                return new OwningPrefabData(current);
            }
        }

        [Serializable]
        struct OverridenPrefabData {
            public HierarchyPath path;
            public Mesh mesh;
            public int rootBone;
            public Material[] materials;
            public ConstantKandraBlendshapes.ConstantBlendshape[] blendshapes;

            OverridenPrefabData(GameObject gameObject, Mesh mesh, int rootBone, Material[] materials, ConstantKandraBlendshapes.ConstantBlendshape[] blendshapes) {
                path = new HierarchyPath(gameObject.transform);
                this.mesh = mesh;
                this.rootBone = rootBone;
                this.materials = materials;
                this.blendshapes = blendshapes;
            }

            public static OverridenPrefabData Create(SkinnedMeshRenderer parent, SkinnedMeshRenderer current) {
                return new OverridenPrefabData(
                    gameObject: current.gameObject,
                    mesh: GetOverride(parent.sharedMesh, current.sharedMesh, (lhs, rhs) => lhs == rhs),
                    rootBone: current.bones.IndexOf(current.rootBone),
                    materials: GetOverride(parent.sharedMaterials, current.sharedMaterials, ArrayUtils.UnityEquals),
                    blendshapes: GetOverride(GetBlendshapes(parent), GetBlendshapes(current), ArrayUtils.Equals)
                );
            }
            
            static T GetOverride<T>(T parent, T current, Func<T, T, bool> equals) {
                return equals(parent, current) ? default : current;
            }

            static ConstantKandraBlendshapes.ConstantBlendshape[] GetBlendshapes(SkinnedMeshRenderer renderer) {
                var mesh = renderer.sharedMesh;
                if (mesh == null) {
                    return Array.Empty<ConstantKandraBlendshapes.ConstantBlendshape>();
                }
                var blendshapes = new List<ConstantKandraBlendshapes.ConstantBlendshape>();
                int blendshapeCount = mesh.blendShapeCount;
                for (ushort i = 0; i < blendshapeCount; i++) {
                    float weight = renderer.GetBlendShapeWeight(i);
                    if (weight != 0) {
                        blendshapes.Add(new ConstantKandraBlendshapes.ConstantBlendshape {
                            index = i,
                            value = weight / 100.0f,
                        });
                    }
                }
                return blendshapes.ToArray();
            }
        }
    }
}