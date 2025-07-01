using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.VFX;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    public static class RenderingContextsCollector {
        static HashSet<RenderingContextObject> s_contextsCache = new();

        [InitializeOnLoadMethod]
        static void RegisterCallbacks() {
            EditorApplication.playModeStateChanged -= ClearData;
            EditorApplication.playModeStateChanged += ClearData;
        }

        public static HashSet<RenderingContextObject> CollectContexts(bool canBeFromCache = true) {
            if (canBeFromCache && s_contextsCache.Count > 0) {
                return s_contextsCache;
            }
            s_contextsCache.Clear();

            if (EditorApplication.isPlaying) {
                CollectPlayMode();
            } else {
                CollectEditMode();
            }

            CollectUniversal();

            return s_contextsCache;
        }

        static void CollectPlayMode() {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var renderer in renderers) {
                ProcessRenderer(renderer);
            }
        }

        static void CollectEditMode() {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var renderer in renderers) {
                ProcessRenderer(renderer);
            }
            var drakeMeshRenderers = Object.FindObjectsByType<DrakeMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var drakeMeshRenderer in drakeMeshRenderers) {
                ProcessDrakeRenderer(drakeMeshRenderer);
            }
            var lodGroups = Object.FindObjectsByType<DrakeLodGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lodGroup in lodGroups) {
                Add(lodGroup, lodGroup.gameObject);
            }
        }

        static void CollectUniversal() {
            var meshColliders = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var meshCollider in meshColliders) {
                if (IsValidTarget(meshCollider.transform)) {
                    Add(meshCollider, meshCollider.gameObject);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessRenderer(Renderer renderer) {
            if (!IsValidTarget(renderer.transform)) {
                return;
            }
            var go = renderer.gameObject;
            if (renderer is MeshRenderer meshRenderer) {
                Add(meshRenderer, go);
                AddMaterials(meshRenderer.sharedMaterials, go);
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null) {
                    return;
                }
                Add(meshFilter, go);
                AddMesh(meshFilter.sharedMesh, go);
            } else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                Add(skinnedMeshRenderer, go);
                AddMaterials(skinnedMeshRenderer.sharedMaterials, go);
                AddMesh(skinnedMeshRenderer.sharedMesh, go);
            } else if (renderer.TryGetComponent<VisualEffect>(out var visualEffect)) {
                Add(visualEffect, go);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessDrakeRenderer(DrakeMeshRenderer renderer) {
            if (!IsValidTarget(renderer.transform)) {
                return;
            }
            var go = renderer.gameObject;
            Add(renderer, go);
            AddMaterials(renderer.MaterialReferences.Select(LoadMaterial).ToArray(), go);
            AddMesh(LoadMesh(renderer.MeshReference), go);

            static Material LoadMaterial(AssetReference materialReference) {
                if (materialReference.IsValid() && materialReference.IsDone) {
                    return materialReference.OperationHandle.Convert<Material>().Result;
                }

                return materialReference.LoadAssetAsync<Material>().WaitForCompletion();
            }

            static Mesh LoadMesh(AssetReference meshReference) {
                if (meshReference.IsValid() && meshReference.IsDone) {
                    return meshReference.OperationHandle.Convert<Mesh>().Result;
                }

                return meshReference.LoadAssetAsync<Mesh>().WaitForCompletion();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AddMaterials(Material[] materials, GameObject sceneObject) {
            foreach (var material in materials) {
                if (material != null) {
                    Add(material, sceneObject);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AddMesh(Mesh mesh, GameObject sceneObject) {
            if (mesh != null) {
                Add(mesh, sceneObject);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Add(Object context, GameObject sceneObject) {
            var dummyContext = new RenderingContextObject(context, null);
            if (s_contextsCache.TryGetValue(dummyContext, out var existingObject)) {
                existingObject.sceneObjects.Add(sceneObject);
            } else {
                var sceneObjects = new HashSet<GameObject>();
                sceneObjects.Add(sceneObject);
                var newObject = new RenderingContextObject(context, sceneObjects);
                s_contextsCache.Add(newObject);
            }
        }

        static bool IsValidTarget(Transform transform) {
            const HideFlags DontSaveFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            var current = transform;
            do {
                if ((current.hideFlags & DontSaveFlags) != 0) {
                    return false;
                }
                current = current.parent;
            }
            while (current != null);

            return true;
        }

        static void ClearData(PlayModeStateChange _) {
            s_contextsCache.Clear();
        }
    }
}
