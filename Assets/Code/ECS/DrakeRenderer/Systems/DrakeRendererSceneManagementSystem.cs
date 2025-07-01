using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Utils;
using Awaken.Utility.Collections;
using Unity.Collections;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Systems {
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DrakeRendererSceneManagementSystem : SystemBase {
        EntityQuery _toUnloadQuery;
        ComponentTypeSet _unloadRemoveSet;

        EntityQuery _toCleanUpQuery;

        protected override void OnCreate() {
            var queryBuilder = new EntityQueryBuilder(ARAlloc.Temp);

            _toUnloadQuery = queryBuilder
                .WithAll<DrakeMeshMaterialComponent>()
                .WithAny<DrakeRendererSpawnedTag, DrakeRendererLoadingTag>()
                .WithNone<SystemRelatedLifeTime<DrakeRendererManager>.IdComponent>()
                .Build(this);
            queryBuilder.Reset();

            _unloadRemoveSet = new ComponentTypeSet(
                ComponentType.ReadOnly<DrakeMeshMaterialComponent>(),
                ComponentType.ReadOnly<DrakeRendererSpawnedTag>(),
                ComponentType.ReadOnly<DrakeRendererLoadingTag>());

            _toCleanUpQuery = queryBuilder
                .WithAll<DrakeMeshMaterialComponent>()
                .WithNone<SystemRelatedLifeTime<DrakeRendererManager>.IdComponent>()
                .Build(this);
            queryBuilder.Dispose();
        }

        protected override void OnUpdate() {
            var toUnload = _toUnloadQuery.ToComponentDataArray<DrakeMeshMaterialComponent>(ARAlloc.Temp);
            var manager = DrakeRendererManager.Instance;
            foreach (var meshMaterialComponent in toUnload) {
                manager.Unload(meshMaterialComponent, false);
            }
            toUnload.Dispose();
            EntityManager.RemoveComponent(_toUnloadQuery, _unloadRemoveSet);
            EntityManager.RemoveComponent<DrakeMeshMaterialComponent>(_toCleanUpQuery);
        }
    }
}
