#if UNITY_EDITOR && ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using UnityEditor;
using UnityEngine;
using Unity.Rendering.Occlusion;
using System.Collections.Generic;
using System;
using System.Linq;
using Unity.Entities;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;

namespace Unity.Rendering.Occlusion
{
    static class OcclusionCommands
    {
        const string kOcclusionMenu = "Occlusion/";

        const string kOcclusionToolsSubMenu = "Tools/";

        const string kOcclusionDebugSubMenu = kOcclusionMenu + "Debug/";

        const string kDebugNone = kOcclusionDebugSubMenu + "None";
        const string kDebugDepth = kOcclusionDebugSubMenu + "Depth buffer";
        const string kDebugShowMeshes = kOcclusionDebugSubMenu + "Show occluder meshes";
        const string kDebugShowBounds = kOcclusionDebugSubMenu + "Show occludee bounds";
        const string kDebugShowTest = kOcclusionDebugSubMenu + "Show depth test";

        const string kOcclusionEnable = kOcclusionMenu + "Enable";
        const string kOcclusionDisplayOccluded = "Occlusion/DisplayOccluded";
        const string kOcclusionParallel = kOcclusionMenu + "Parallel Rasterization";

        [MenuItem(kOcclusionEnable, false)]
        static void ToggleOcclusionEnable()
        {
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                var occlusionCulling = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>().OcclusionCulling;
                occlusionCulling.IsEnabled = !occlusionCulling.IsEnabled;

                OcclusionBrowseWindow.Refresh();
            }
        }

        [MenuItem(kOcclusionEnable, true)]
        static bool ValidateOcclusionEnable()
        {
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                var occlusionCulling = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>().OcclusionCulling;
                Menu.SetChecked(kOcclusionEnable, occlusionCulling.IsEnabled);
            }
            return true;
        }

        static void AddComponentIfNeeded<T>(this MeshRenderer meshRenderer) where T : MonoBehaviour
        {
            var gameObject = meshRenderer.gameObject;
            if (!gameObject.TryGetComponent<T>(out var occluder))
            {
                occluder = gameObject.AddComponent<T>();
                occluder.enabled = true;
            }
        }

        static void DestroyComponentIfNeeded<T>(this MeshRenderer meshRenderer) where T : MonoBehaviour
        {
            var gameObject = meshRenderer.gameObject;
            if (gameObject.TryGetComponent<T>(out var occluder))
                Object.DestroyImmediate(occluder);
        }

        [MenuItem(kOcclusionMenu + kOcclusionToolsSubMenu + "Add occlusion components to all open scenes and objects")]
        static void AddAllOcclusionComponents()
        {
            ForEachRenderer((meshRenderer) =>
            {
                meshRenderer.AddComponentIfNeeded<Occluder>();
            }, OccluderEditMode.AllObjects);
        }

        [MenuItem(kOcclusionMenu + kOcclusionToolsSubMenu + "Remove occlusion components from all open scenes and objects")]
        static void RemoveAllOcclusionComponents()
        {
            ForEachRenderer((meshRenderer) =>
            {
                meshRenderer.DestroyComponentIfNeeded<Occluder>();
            }, OccluderEditMode.AllObjects);
        }

        [MenuItem(kOcclusionMenu + kOcclusionToolsSubMenu + "Add occlusion components to selected")]
        static void AddOcclusionComponentsToSelected()
        {
            ForEachRenderer((meshRenderer) =>
            {
                meshRenderer.AddComponentIfNeeded<Occluder>();
            }, OccluderEditMode.SelectedObjects);
        }

        [MenuItem(kOcclusionMenu + kOcclusionToolsSubMenu + "Remove occlusion components from selected")]
        static void RemoveOcclusionComponentsFromSelected()
        {
            ForEachRenderer((meshRenderer) =>
            {
                meshRenderer.DestroyComponentIfNeeded<Occluder>();
            }, OccluderEditMode.SelectedObjects);
        }

        [MenuItem(kOcclusionMenu + kOcclusionToolsSubMenu + "Add occluder component to selected")]
        static void AddOccluderComponentToSelected()
        {
            ForEachRenderer((meshRenderer) =>
            {
                meshRenderer.AddComponentIfNeeded<Occluder>();
            }, OccluderEditMode.SelectedObjects);
        }

        [MenuItem(kOcclusionMenu + kOcclusionToolsSubMenu + "Add occluder component to all open scenes and objects")]
        static void AddOccluderComponentToAll()
                {
            ForEachRenderer((meshRenderer) =>
            {
                meshRenderer.AddComponentIfNeeded<Occluder>();
            }, OccluderEditMode.AllObjects);
        }

        enum OccluderEditMode
        {
            AllObjects,
            SelectedObjects,
        }

        static void ForEachRenderer(Action<MeshRenderer> action, OccluderEditMode mode)
        {
            var renderers = mode == OccluderEditMode.AllObjects
                ? Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                : Selection.gameObjects.SelectMany(x => x.GetComponents<MeshRenderer>());
            renderers = renderers.Distinct();

            foreach (var renderer in renderers)
            {
                action(renderer);
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
            }
        }
    }
}
#endif

