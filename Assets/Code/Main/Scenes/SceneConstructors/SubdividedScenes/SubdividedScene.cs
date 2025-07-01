using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Debugging;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes {
    public class SubdividedScene : MapScene, ISubscenesOwner {
        [SerializeField, BoxGroup] SerializedSubscenesData serializedSubscenesData;
        
        [InfoBox("Use \"TG/Build/Subdivide Selected\" with this scene selected in Project window", VisibleIf = "@!mapStaticScene.IsSet")]
        [SerializeField]
        public SceneReference mapStaticScene;

        [SerializeField] public List<SceneReference> staticSubscenes = new();

        public override Scene[] UnityScenes => AllLoadedUnityScenes();
        public Scene Scene { get; private set; }
        public IEnumerable<(string, SceneReference)> AllScenesWithPath => serializedSubscenesData.AllScenesWithPath;
        public int SubscenesCount => serializedSubscenesData.SubscenesCount;

        public bool EnableFastReloading = true;
        public event System.Action<SubdividedScene, List<SceneReference>> OnLoadedAllSubscenes;
        
        protected override UniTaskVoid InitAll() {
            Scene = gameObject.scene;
            var nonStaticSubscenesToLoad = serializedSubscenesData.GetAllScenes(false).ToArray();
            var allSubscenesScenesToLoad = new List<SceneReference>(nonStaticSubscenesToLoad.Length + (staticSubscenes.Count == 0 ? 0 : nonStaticSubscenesToLoad.Length));
            allSubscenesScenesToLoad.AddRange(nonStaticSubscenesToLoad);
            allSubscenesScenesToLoad.AddRange(staticSubscenes.Where(x =>
                ContainsSceneWithName(nonStaticSubscenesToLoad, GetNonStaticSceneName(x.Name))));
            var enumerator = allSubscenesScenesToLoad.GetEnumerator();
            var distanceCullersToInitialize = new List<DistanceCuller>(allSubscenesScenesToLoad.Capacity + 2);
            if (IsMapStaticSceneLoaded()) {
                aStarPath = GameObjects.FindComponentByTypeInScene<AstarPath>(mapStaticScene.LoadedScene, true);
                LoadNextScene();
            } else {
                if (!IsMapStaticSceneValid()) {
                    Log.Debug?.Error("Map static scene reference is null");
                    LoadNextScene();
                } else {
                    SceneLoadOperation operation = SceneService.LoadSceneAsync(mapStaticScene, LoadSceneMode.Additive);
                    operation.OnComplete(() => {
                        // Specific case but it is okay that mapStaticScene is owner of itself. It is needed to not unload 
                        // mapStaticScene when Scene is unloaded.
                        SetSubSceneOwner(mapStaticScene.LoadedScene, mapStaticScene.LoadedScene);
                        aStarPath = GameObjects.FindComponentByTypeInScene<AstarPath>(mapStaticScene.LoadedScene, true);
                        LoadNextScene();
                    });
                }
            }

            return default;

            void LoadNextScene() {
                if (this == null) {
                    return;
                }

                if (enumerator.MoveNext()) {
                    try {
                        if (enumerator.Current.LoadedScene.IsValid()) {
                            if (!staticSubscenes.Contains(enumerator.Current)) {
                                Log.Debug?.Error($"Non static scene {enumerator.Current.Name} was not unloaded properly");
                                FailAndReturnToTitleScreen().Forget();
                                return;
                            }

                            SetSubSceneOwner(enumerator.Current.LoadedScene, mapStaticScene.LoadedScene);
                            LoadNextScene();
                        } else {
                            SceneLoadOperation operation = SceneService.LoadSceneAsync(enumerator.Current, LoadSceneMode.Additive);
                            operation.OnComplete(() => {
                                var loadedScene = enumerator.Current.LoadedScene;
                                var ownerScene = staticSubscenes.Contains(enumerator.Current)
                                    ? mapStaticScene.LoadedScene
                                    : Scene;
                                SetSubSceneOwner(loadedScene, ownerScene);
                                var distanceCuller = GameObjects.FindComponentByTypeInScene<DistanceCuller>(loadedScene, false);
                                if (distanceCuller != null) {
                                    distanceCullersToInitialize.Add(distanceCuller);
                                }
                                LoadNextScene();
                            });
                        }
                    } catch (Exception e) {
                        Log.Important?.Error($"Exception below happened while loading scene {enumerator.Current.Name}");
                        Debug.LogException(e);
                        FailAndReturnToTitleScreen().Forget();
                    }
                } else {
                    DistanceCuller distanceCuller;
                    if (IsMapStaticSceneLoaded()) {
                        distanceCuller = GameObjects.FindComponentByTypeInScene<DistanceCuller>(mapStaticScene.LoadedScene, false);
                        if (distanceCuller != null) {
                            distanceCullersToInitialize.Add(distanceCuller);
                        }
                    }
                    OnLoadedAllSubscenes?.Invoke(this, allSubscenesScenesToLoad);
                    distanceCuller = GameObjects.FindComponentByTypeInScene<DistanceCuller>(gameObject.scene, false);
                    if (distanceCuller != null) {
                        distanceCullersToInitialize.Add(distanceCuller);
                    }
                    foreach (var distanceCullerToInitialize in distanceCullersToInitialize) {
                        distanceCullerToInitialize.Initialize();
                    }

                    distanceCullersToInitialize.Clear();
                    distanceCullersToInitialize = null;

                    allSubscenesScenesToLoad.Clear();
                    allSubscenesScenesToLoad = null;

                    nonStaticSubscenesToLoad = null;

                    enumerator.Dispose();
                    enumerator = default;
                    base.InitAll().Forget();
                }
            }
        }

        public IEnumerable<SceneReference> GetAllScenes(bool ignoreEditorPrefs) {
            return serializedSubscenesData.GetAllScenes(ignoreEditorPrefs);
        }
        
        public override ISceneLoadOperation Unload(bool isSameSceneReloading) {
            IsInitialized = false;
            isSameSceneReloading &= EnableFastReloading;
            bool doUnloadStatic = !isSameSceneReloading;
            List<SceneLoadOperation> operationsList = new(staticSubscenes.Count + 20);
            if (doUnloadStatic) {
                operationsList.AddRange(staticSubscenes
                    .Where(x => x.LoadedScene.IsValid())
                    .Select(SceneService.UnloadSceneAsync));
                if (mapStaticScene != null && mapStaticScene.IsSet) {
                    operationsList.Add(SceneService.UnloadSceneAsync(mapStaticScene));
                }
            }

            operationsList.AddRange(GetAllScenes(false)
                .Where(s => s.LoadedScene.IsValid())
                .Select(SceneService.UnloadSceneAsync));
            return new SubdividedSceneUnloadOperation(SceneRef, operationsList.ToArray());
        }

        bool IsMapStaticSceneValid() => mapStaticScene != null && mapStaticScene.IsSet;
        bool IsMapStaticSceneLoaded() => IsMapStaticSceneValid() && mapStaticScene.LoadedScene.IsValid();

        static void SetSubSceneOwner(Scene loadedSubScene, Scene ownerScene) {
            var rootGO = loadedSubScene.GetRootGameObjects();
            int count = rootGO.Length;
            for (int i = 0; i < count; i++) {
                if (rootGO[i].TryGetComponent(out ISubscene mapScene)) {
                    mapScene.OwnerScene = ownerScene;
                    return;
                }
            }
        }
        
        static bool ContainsSceneWithName(SceneReference[] sceneReferences, string name) {
            foreach (var sceneRef in sceneReferences) {
                if (sceneRef.Name == name) {
                    return true;
                }
            }

            return false;
        }
        
        static string GetNonStaticSceneName(string staticSceneName) {
            return staticSceneName.Remove(staticSceneName.Length - SceneService.StaticSceneSuffix.Length,
                SceneService.StaticSceneSuffix.Length);
        }

        Scene[] AllLoadedUnityScenes() {
            var subdividedSceneChildren = FindObjectsByType<SubdividedSceneChild>(FindObjectsSortMode.None);
            var scenes = new Scene[subdividedSceneChildren.Length + 1];
            scenes[0] = gameObject.scene;
            var index = 0;
            foreach (var child in subdividedSceneChildren) {
                scenes[++index] = child.gameObject.scene;
            }
            return scenes;
        }

#if UNITY_EDITOR
        [Button]
        public void RefreshStaticScenesList() {
            staticSubscenes = new(20);
            var mapStaticScenePath = gameObject.scene.path.Insert(gameObject.scene.path.Length - ".unity".Length, SceneService.StaticSceneSuffix);
            if (UnityEditor.AssetDatabase.AssetPathExists(mapStaticScenePath)) {
                mapStaticScene = SceneReference.ByAddressable(new ARAssetReference(UnityEditor.AssetDatabase.AssetPathToGUID(mapStaticScenePath)));
            } else {
                mapStaticScene = null;
            }

            foreach (var subScene in GetAllScenes(true)) {
                TryAddStaticSceneToList(subScene);
            }

            void TryAddStaticSceneToList(SceneReference subScene) {
                var assetRef = new SceneReference.EditorAccess(subScene).Reference;
                var subScenePath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetRef.Address);
                var staticScenePath = subScenePath.Insert(subScenePath.Length - ".unity".Length, SceneService.StaticSceneSuffix);
                if (!UnityEditor.AssetDatabase.AssetPathExists(staticScenePath)) {
                    return;
                }

                var staticSceneGuid = UnityEditor.AssetDatabase.AssetPathToGUID(staticScenePath);
                staticSubscenes.Add(SceneReference.ByAddressable(new ARAssetReference(staticSceneGuid)));
            }
        }
#endif
        
        [Serializable]
        public struct SubsceneData {
            public int stableUniqueId;
            public int id;
            public string nameOverride;
            public SceneReference reference;
            public string Name => string.IsNullOrWhiteSpace(nameOverride) ? reference.Name : nameOverride;
        }

        [Serializable]
        public struct SerializedSubscenesData {
            public const string EditorPrefNameDoNotLoadScene = "do_not_load_subscene_";
            public const string EditorPrefNameDoNotLoadNode = "do_not_load_node_";
            
            public SubsceneData[] Scenes;
            public NodeData[] Nodes;
            public int NodesUniqueIdCounter;
            public int ScenesUniqueIdCounter;
            public int SubscenesCount => Scenes?.Length ?? 0;
            [UnityEngine.Scripting.Preserve] public bool IsEmpty => (Scenes == null || Scenes.Length == 0) && (Nodes == null || Nodes.Length == 0);
            public readonly IEnumerable<(string, SceneReference)> AllScenesWithPath => GetAllScenesWithPath(0, "");
            
            public readonly IEnumerable<SceneReference> GetAllScenes(bool ignoreEditorPrefs) {
                return GetAllScenes(0, ignoreEditorPrefs);
            }

            public readonly IEnumerable<SceneReference> GetAllScenes(int nodeIndex, bool ignoreEditorPrefs) {
                var node = Nodes[nodeIndex];
#if UNITY_EDITOR && !SIMULATE_BUILD
                if (ignoreEditorPrefs == false &&
                    UnityEditor.EditorPrefs.GetBool(EditorPrefNameDoNotLoadNode + node.stableUniqueId) &&
                    UnityEditor.EditorPrefs.GetBool("Baking") == false) {
                    yield break;
                }
#endif
                int childNodesCount = node.childNodesCount;
                for (int i = 0; i < childNodesCount; i++) {
                    var childNodeIndex = node.firstChildNodeIndex + i;
                    foreach (var sceneReference in GetAllScenes(childNodeIndex, ignoreEditorPrefs)) {
                        yield return sceneReference;
                    }
                }

                int childScenesCount = node.childScenesCount;
                for (int i = 0; i < childScenesCount; i++) {
                    var childSceneIndex = node.firstChildSceneIndex + i;
                    var scene = Scenes[childSceneIndex];
                    if (scene.reference.IsSet
#if UNITY_EDITOR && !SIMULATE_BUILD
                        && (ignoreEditorPrefs ||
                            UnityEditor.EditorPrefs.GetBool(EditorPrefNameDoNotLoadScene + scene.stableUniqueId) == false
                            || UnityEditor.EditorPrefs.GetBool("Baking"))
#endif
                       ) {
                        if (!Configuration.GetBool($"disable_scene.{scene.reference.Name}")) {
                            yield return scene.reference;
                        }
                    }
                }
            }

            public static int GetId(short sceneIndex, short parentNodeIndex) {
                return (sceneIndex << 16) | (parentNodeIndex & 0xFFFF);
            }

            public static (short sceneIndex, short parentNodeIndex) DeconstructId(int id) {
                return (GetIndexFromId(id), GetParentNodeIndex(id));
            }

            public static short GetIndexFromId(int id) {
                return (short)(id >> 16);
            }

            public static short GetParentNodeIndex(int id) {
                return (short)(id & 0xFFFF);
            }

            readonly IEnumerable<(string, SceneReference)> GetAllScenesWithPath(int nodeIndex, string path) {
                var node = Nodes[nodeIndex];
                int childNodesCount = node.childNodesCount;
                for (int i = 0; i < childNodesCount; i++) {
                    var childNodeIndex = node.firstChildNodeIndex + i;
                    foreach (var result in GetAllScenesWithPath(childNodeIndex, AppendPath(path, node.name))) {
                        yield return result;
                    }
                }

                int childScenesCount = node.childScenesCount;
                for (int i = 0; i < childScenesCount; i++) {
                    var childSceneIndex = node.firstChildSceneIndex + i;
                    var scene = Scenes[childSceneIndex];
                    if (scene.reference.IsSet) {
                        yield return (AppendPath(path, scene.Name), scene.reference);
                    }
                }
            }

            static string AppendPath(string path, string name) {
                if (string.IsNullOrWhiteSpace(path)) {
                    return name;
                }

                return $"{path} - {name}";
            }
            
            [Serializable]
            public struct NodeData {
                public short firstChildNodeIndex;
                public short childNodesCount;
                public short firstChildSceneIndex;
                public short childScenesCount;
                public int id;
                public int stableUniqueId;
                public string name;
                [UnityEngine.Scripting.Preserve] public bool ContainsScenes => childScenesCount != 0;
                [UnityEngine.Scripting.Preserve] public bool ContainsNodes => childNodesCount != 0;
                [UnityEngine.Scripting.Preserve] public short NodeIndex => GetIndexFromId(id);
                [UnityEngine.Scripting.Preserve] public short ParentNodeIndex => GetParentNodeIndex(id);
            }

            struct ListArray<T> {
                T[] _array;
                int _count;
                [UnityEngine.Scripting.Preserve] public int Count => _count;
                [UnityEngine.Scripting.Preserve] public T[] RawArray => _array;

                [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
                [UnityEngine.Scripting.Preserve]
                public ref T this[int index] => ref _array[index];

                public ListArray(int length) {
                    _array = new T[length];
                    _count = 0;
                }

                [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
                [UnityEngine.Scripting.Preserve]
                public void Add(T element) {
                    _array[_count] = element;
                    _count++;
                }
            }
        }
#if UNITY_EDITOR
        public struct EditorAccess {
            readonly SubdividedScene _subdividedScene;
            readonly ref SerializedSubscenesData Subscenes => ref _subdividedScene.serializedSubscenesData;

            public EditorAccess(SubdividedScene subdividedScene) {
                _subdividedScene = subdividedScene;
            }

            public readonly void LoadAllScenes(bool ignoreEditorPrefs, List<Scene> loadedSubscenes = null) {
                foreach (var sceneRef in Subscenes.GetAllScenes(ignoreEditorPrefs)) {
                    var loadedSubscene = new SceneReference.EditorAccess(sceneRef).LoadScene();
                    if (loadedSubscenes != null) {
                        loadedSubscenes.Add(loadedSubscene);
                    }
                }
            }
            
            public readonly void GetLoadedScenes(bool ignoreEditorPrefs, List<Scene> result) {
                foreach (var sceneRef in Subscenes.GetAllScenes(ignoreEditorPrefs)) {
                    var loadedScene = new SceneReference.EditorAccess(sceneRef).LoadedScene;
                    if (loadedScene.IsValid() && loadedScene.isLoaded) {
                        result.Add(loadedScene);
                    }
                }
            }
            
            public readonly void UnloadAllScenes(bool ignoreEditorPrefs, bool withSave) {
                foreach (var sceneRef in Subscenes.GetAllScenes(ignoreEditorPrefs)) {
                    new SceneReference.EditorAccess(sceneRef).UnloadScene(withSave);
                }
            }

            public void ReplaceSubscenesWithMerged(SceneReference mergedScene) {
                Subscenes.Scenes = new SubsceneData[1] {
                    new SubsceneData() {
                        reference = mergedScene,
                        id = 0,
                        stableUniqueId = 0,
                    }
                };
                Subscenes.Nodes = new SerializedSubscenesData.NodeData[1] {
                    new SerializedSubscenesData.NodeData() {
                        childNodesCount = 0,
                        childScenesCount = 1,
                        firstChildNodeIndex = 0,
                        firstChildSceneIndex = 0,
                        id = 0,
                        name = "Merged",
                        stableUniqueId = 0,
                    }
                };
                Subscenes.NodesUniqueIdCounter = 1;
                Subscenes.ScenesUniqueIdCounter = 1;
            }
        }
#endif
    }
}