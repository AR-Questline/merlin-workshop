using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Mipmaps.Components;
using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Systems {
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class DrakeRendererLoadingSystem : SystemBase {
        EntityQuery _startLoadingQuery;
        EntityQuery _releaseResourcesQuery;
        EntityQuery _loadingEntitiesQuery;
        ComponentTypeSet _releaseResourcesRemoveSet;

        public bool IsLoadingAnyEntities => _loadingEntitiesQuery.CalculateChunkCount() != 0;
        public ref readonly ComponentTypeSet ReleaseResourcesRemoveSet => ref _releaseResourcesRemoveSet;

        protected override void OnCreate() {
            _releaseResourcesRemoveSet = new ComponentTypeSet(new[] {
                ComponentType.ReadOnly<DrakeRendererUnloadRequestTag>(),
                ComponentType.ReadOnly<MaterialMeshInfo>(),
                ComponentType.ReadOnly<DrakeRendererSpawnedTag>(),
                ComponentType.ReadOnly<MipmapsMaterialComponent>(),
                ComponentType.ReadOnly<UVDistributionMetricComponent>(),
                ComponentType.ChunkComponent<EntitiesGraphicsChunkInfo>()
            });
            _loadingEntitiesQuery = GetEntityQuery(new EntityQueryDesc() {
                All = new[] { ComponentType.ReadOnly<DrakeMeshMaterialComponent>() },
                Any = new[] { 
                    ComponentType.ReadOnly<DrakeRendererLoadingTag>(), 
                    ComponentType.ReadOnly<DrakeRendererLoadRequestTag>() }
            });
        }

        protected override void OnUpdate() {
            var unmanaged = DrakeRendererManager.Instance.GetUnmanaged();
            var ecb = new EntityCommandBuffer(ARAlloc.TempJob, PlaybackPolicy.SinglePlayback);
            var passedEntities = new NativeList<Entity>(64, ARAlloc.TempJob);

            new CheckAndAssignLoadingRenderersResourcesJob {
                unmanaged = unmanaged,
                ecb = ecb,
                passedEntities = passedEntities,
            }.Run(SystemAPI.QueryBuilder().WithPresent<DrakeMeshMaterialComponent, DrakeRendererLoadingTag>().Build());

            ecb.Playback(EntityManager);
            ecb.Dispose();

            var passedEntitiesArray = passedEntities.AsArray();
            EntityManager.AddComponent<DrakeRendererSpawnedTag>(passedEntitiesArray);
            EntityManager.RemoveComponent<DrakeRendererLoadingTag>(passedEntitiesArray);

            passedEntities.Dispose();

            Entities
                .WithAll<DrakeRendererLoadRequestTag>()
                .ForEach((in DrakeMeshMaterialComponent meshMaterialComponent) => {
                    var manager = DrakeRendererManager.Instance;
                    manager.StartLoading(meshMaterialComponent);
                })
                .WithName("Start_Loading_Rendering_Resources")
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref _startLoadingQuery)
                .Run();
            EntityManager.AddComponent<DrakeRendererLoadingTag>(_startLoadingQuery);
            EntityManager.RemoveComponent<DrakeRendererLoadRequestTag>(_startLoadingQuery);

            Entities
                .WithAll<DrakeRendererUnloadRequestTag, MaterialMeshInfo, DrakeRendererSpawnedTag>()
                .ForEach((in DrakeMeshMaterialComponent meshMaterialComponent) => {
                    var manager = DrakeRendererManager.Instance;
                    manager.Unload(meshMaterialComponent);
                })
                .WithName("Release_Rendering_Resources")
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref _releaseResourcesQuery)
                .Run();
            EntityManager.RemoveComponent(_releaseResourcesQuery, _releaseResourcesRemoveSet);
        }

        [BurstCompile]
        partial struct CheckAndAssignLoadingRenderersResourcesJob : IJobEntity {
            public DrakeRendererManager.Unmanaged unmanaged;
            public EntityCommandBuffer ecb;
            public NativeList<Entity> passedEntities;

            public void Execute(Entity entity, in DrakeMeshMaterialComponent meshMaterialComponent) {
                if (!unmanaged.TryGetMaterialMesh(meshMaterialComponent, out var meshMaterialInfo, out var mipmapsMaterialComponent, out var uvDistributionMetricComponent)) {
                    return;
                }

                ecb.AddComponent(entity, meshMaterialInfo);
                ecb.AddComponent(entity, mipmapsMaterialComponent);
                ecb.AddComponent(entity, uvDistributionMetricComponent);

                passedEntities.Add(entity);
            }
        }
    }
}
