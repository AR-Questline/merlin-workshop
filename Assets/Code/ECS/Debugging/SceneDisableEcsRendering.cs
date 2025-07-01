using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.Utils;
using Awaken.Utility.Debugging;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.ECS.Debugging {
    internal static class SceneDisableEcsRendering {
        static readonly ComponentType[] DisableRenderingTypes = { typeof(DisableRendering), typeof(ARDisabledRenderingTag) };
        static readonly ComponentTypeSet DisableRenderingSet = new ComponentTypeSet(DisableRenderingTypes);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterButtons() {
            ScenesDebuggingWindow.DefinedButtons.Add(new("Disable ecs rendering", DisableDrakeAtScene, true, true));
        }

        static void DisableDrakeAtScene(Scene scene) {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] { typeof(MaterialMeshInfo), typeof(SystemRelatedLifeTime<DrakeRendererManager>.IdComponent) },
                Absent = DisableRenderingTypes,
            });
            var idComponent = new SystemRelatedLifeTime<DrakeRendererManager>.IdComponent(scene.handle);
            query.SetSharedComponentFilter(idComponent);

            entityManager.AddComponent(query, DisableRenderingSet);
        }
    }
}
