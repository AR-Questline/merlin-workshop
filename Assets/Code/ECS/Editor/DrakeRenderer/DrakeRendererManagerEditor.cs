using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeRendererManagerEditor {
        public static readonly HashSet<DrakeMeshRenderer> DrakeMeshRenderers = new HashSet<DrakeMeshRenderer>();
        public static readonly HashSet<DrakeLodGroup> DrakeLodGroups = new HashSet<DrakeLodGroup>();

        static readonly HashSet<DrakeMeshRendererTransformPair> DrakeMeshRenderersToAdd = new HashSet<DrakeMeshRendererTransformPair>();
        static readonly HashSet<DrakeMeshRendererTransformPair> DrakeMeshRenderersToRemove = new HashSet<DrakeMeshRendererTransformPair>();
        static readonly HashSet<DrakeLodGroupTransformPair> DrakeLodGroupsToAdd = new HashSet<DrakeLodGroupTransformPair>();
        static readonly HashSet<DrakeLodGroupTransformPair> DrakeLodGroupsToRemove = new HashSet<DrakeLodGroupTransformPair>();

        static Tool? s_lastTool;

        public static event Action<HashSet<DrakeLodGroupTransformPair>> AddedDrakeLodGroups;
        public static event Action<HashSet<DrakeLodGroupTransformPair>> RemovedDrakeLodGroups;
        public static event Action<HashSet<DrakeMeshRendererTransformPair>> AddedDrakeMeshRenderers;
        public static event Action<HashSet<DrakeMeshRendererTransformPair>> RemovedDrakeMeshRenderer;
        
        public static void EDITOR_RuntimeReset() {
            DrakeMeshRenderers.Clear();
            DrakeLodGroups.Clear();
            DrakeMeshRenderersToAdd.Clear();
            DrakeMeshRenderersToRemove.Clear();
            DrakeLodGroupsToAdd.Clear();
            DrakeLodGroupsToRemove.Clear();
            s_lastTool = null;
        }
        
        [InitializeOnLoadMethod]
        static void EditorInit() {
            DrakeHackToolbarButton.SceneAuthoringHackChanged += OnSceneAuthoringHackChanged;
            DrakeHighestLodToolbarButton.HighestLodModeChanged += ReloadDrake;

            DrakeLodGroup.OnAddedDrakeLodGroup += OnAddedDrakeLodGroup;
            DrakeMeshRenderer.OnAddedDrakeMeshRenderer += OnAddedDrakeMeshRenderer;
            DrakeLodGroup.OnRemovedDrakeLodGroup += OnRemovedDrakeLodGroup;
            DrakeMeshRenderer.OnRemovedDrakeMeshRenderer += OnRemovedDrakeMeshRenderer;

            PrefabStage.prefabSaved += OnPrefabSaved;
            EditorApplication.update += OnEditorUpdate;

            AssignOcclusionCullingCreators();
        }

        public static void AfterBootstrap() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
            var scenesCount = SceneManager.sceneCount;
            for (var i = 0; i < scenesCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) {
                    BootstrapScene(scene);
                }
            }

            if (!DrakeHackToolbarButton.SceneAuthoringHack) {
                DrakeRendererEntitiesManagerEditor.Start();
            } else {
                DrakeRendererAuthoringHackManagerEditor.Start();
            }
        }
        
        public static bool IsPartOfEditingPrefab(GameObject gameObject, GameObject editingPrefab) {
            do {
                gameObject = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
                if (gameObject == null) {
                    break;
                }
                var rootAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
                if (rootAsset == editingPrefab) {
                    return true;
                }
                gameObject = gameObject.transform.parent?.gameObject;
            }
            while (gameObject != null);
            return false;
        }

        static void OnSceneAuthoringHackChanged() {
            if (!DrakeHackToolbarButton.SceneAuthoringHack) {
                DrakeRendererAuthoringHackManagerEditor.Stop();
                DrakeRendererEntitiesManagerEditor.Start();
            } else {
                DrakeRendererEntitiesManagerEditor.Stop();
                DrakeRendererAuthoringHackManagerEditor.Start();
            }
        }

        static void ReloadDrake() {
            ReloadDrakeAsync().Forget();
        }

        static async UniTaskVoid ReloadDrakeAsync() {
            if (!DrakeHackToolbarButton.SceneAuthoringHack) {
                DrakeRendererEntitiesManagerEditor.Stop();
                await UniTask.DelayFrame(2);
                DrakeRendererEntitiesManagerEditor.Start();
            } else {
                DrakeRendererAuthoringHackManagerEditor.Stop();
                await UniTask.DelayFrame(2);
                DrakeRendererAuthoringHackManagerEditor.Start();
            }
        }

        static void OnAddedDrakeLodGroup(DrakeLodGroup drakeLodGroup) {
            if (EditorApplication.isCompiling) {
                return;
            }

            DrakeLodGroupsToAdd.Add(drakeLodGroup);
            DrakeLodGroupsToRemove.Remove(drakeLodGroup);
        }

        static void OnAddedDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            if (EditorApplication.isCompiling) {
                return;
            }

            DrakeMeshRenderersToAdd.Add(drakeMeshRenderer);
            DrakeMeshRenderersToRemove.Remove(drakeMeshRenderer);
        }

        static void OnRemovedDrakeLodGroup(DrakeLodGroup drakeLodGroup) {
            if (EditorApplication.isCompiling) {
                return;
            }

            DrakeLodGroupsToRemove.Add(drakeLodGroup);
            DrakeLodGroupsToAdd.Remove(drakeLodGroup);
        }

        static void OnRemovedDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            if (EditorApplication.isCompiling) {
                return;
            }

            DrakeMeshRenderersToRemove.Add(drakeMeshRenderer);
            DrakeMeshRenderersToAdd.Remove(drakeMeshRenderer);
        }

        static void OnPrefabSaved(GameObject _) {
            DrakeMeshRenderers.RemoveWhere(static dr => !dr);
            DrakeLodGroups.RemoveWhere(static dr => !dr);
        }

        static void OnEditorUpdate() {
            if (IsSelectedPartOfNonEditable()) {
                if (Tools.current != Tool.None) {
                    s_lastTool = Tools.current;
                }
                Tools.current = Tool.None;
            } else if (s_lastTool.HasValue) {
                Tools.current = s_lastTool.Value;
                s_lastTool = null;
            }

            BulkUpdateObjects();
        }

        static void BulkUpdateObjects() {
            DrakeLodGroupsToAdd.RemoveWhere(static dr => !dr.drakeLodGroup || !DrakeLodGroups.Add(dr.drakeLodGroup));
            if (DrakeLodGroupsToAdd.Count > 0) {
                AddedDrakeLodGroups?.Invoke(DrakeLodGroupsToAdd);
                DrakeLodGroupsToAdd.Clear();
            }

            DrakeLodGroupsToRemove.RemoveWhere(static dr => !DrakeLodGroups.Remove(dr.drakeLodGroup));
            if (DrakeLodGroupsToRemove.Count > 0) {
                RemovedDrakeLodGroups?.Invoke(DrakeLodGroupsToRemove);
                DrakeLodGroupsToRemove.Clear();
            }

            DrakeMeshRenderersToAdd.RemoveWhere(static dr => !dr.drakeMeshRenderer || !DrakeMeshRenderers.Add(dr.drakeMeshRenderer));
            if (DrakeMeshRenderersToAdd.Count > 0) {
                AddedDrakeMeshRenderers?.Invoke(DrakeMeshRenderersToAdd);
                DrakeMeshRenderersToAdd.Clear();
            }

            DrakeMeshRenderersToRemove.RemoveWhere(static dr => !DrakeMeshRenderers.Remove(dr.drakeMeshRenderer));
            if (DrakeMeshRenderersToRemove.Count > 0) {
                RemovedDrakeMeshRenderer?.Invoke(DrakeMeshRenderersToRemove);
                DrakeMeshRenderersToRemove.Clear();
            }
        }

        static void BootstrapScene(Scene scene) {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots) {
                if (!root.activeSelf) {
                    continue;
                }
                var lods = root.GetComponentsInChildren<DrakeLodGroup>();
                foreach (var lod in lods) {
                    OnAddedDrakeLodGroup(lod);
                }
                var renderers = root.GetComponentsInChildren<DrakeMeshRenderer>();
                foreach (var renderer in renderers) {
                    OnAddedDrakeMeshRenderer(renderer);
                }
            }
        }

        static bool IsSelectedPartOfNonEditable() {
            var target = Selection.activeGameObject;
            var upTo = target;
            if (target) {
                upTo = FindSelectionDrakeRoot(target, upTo);
            } else {
                return false;
            }

            while (target != upTo) {
                if (target!.hideFlags.HasFlag(HideFlags.NotEditable)) {
                    return true;
                }
                target = target.transform.parent?.gameObject;
            }

            return false;
        }

        static GameObject FindSelectionDrakeRoot(GameObject target, GameObject upTo) {
            var lodGroup = target.GetComponentInParent<DrakeLodGroup>();
            if (lodGroup) {
                upTo = lodGroup.gameObject;
            } else {
                var drakeRenderer = target.GetComponentInParent<DrakeMeshRenderer>();
                if (drakeRenderer) {
                    upTo = drakeRenderer.gameObject;
                }
            }
            return upTo;
        }
        
        static void AssignOcclusionCullingCreators() {
            DrakeLodGroup.OnEnterOcclusionCullingCreator = LodGroupEnterOcclusionCullingCreator;
            DrakeMeshRenderer.OnEnterOcclusionCullingCreator = MeshRendererEnterOcclusionCullingCreator;
        }

        static IWithOcclusionCullingTarget.IRevertOcclusion LodGroupEnterOcclusionCullingCreator(DrakeLodGroup drakeLodGroup) {
            return new OcclusionCullingLodGroup(drakeLodGroup);
        }
        
        static IWithOcclusionCullingTarget.IRevertOcclusion MeshRendererEnterOcclusionCullingCreator(DrakeMeshRenderer drakeMeshRenderer) {
            if (drakeMeshRenderer.Parent) {
                return IWithOcclusionCullingTarget.TargetRevertDummy;
            }
            return new OcclusionCullingRenderer(drakeMeshRenderer);
        }

        class OcclusionCullingLodGroup : IWithOcclusionCullingTarget.IRevertOcclusion {
            GameObject _spawned;
            
            public OcclusionCullingLodGroup(DrakeLodGroup drakeLodGroup) {
                _spawned = new GameObject(drakeLodGroup.name + " Occlusion Culling");
                _spawned.transform.SetParent(drakeLodGroup.transform, false);
                _spawned.gameObject.isStatic = drakeLodGroup.gameObject.isStatic;
                drakeLodGroup.BakeStatic();
                DrakeEditorHelpers.SpawnAuthoring(drakeLodGroup, _spawned);
            }
            
            public void Revert() {
                if (_spawned) {
                    Object.DestroyImmediate(_spawned);
                }
            }
        }
        
        class OcclusionCullingRenderer : IWithOcclusionCullingTarget.IRevertOcclusion {
            GameObject _spawned;
            
            public OcclusionCullingRenderer(DrakeMeshRenderer drakeMeshRenderer) {
                _spawned = new GameObject(drakeMeshRenderer.name + " Occlusion Culling");
                _spawned.transform.SetParent(drakeMeshRenderer.transform, false);
                _spawned.gameObject.isStatic = drakeMeshRenderer.gameObject.isStatic;
                drakeMeshRenderer.BakeStatic();
                DrakeMeshRendererEditor.SpawnAuthoring(drakeMeshRenderer, _spawned);
            }
            
            public void Revert() {
                if (_spawned) {
                    Object.DestroyImmediate(_spawned);
                }
            }
        }
    }
}
