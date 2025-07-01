using Awaken.ECS.Components;
using Awaken.Utility.Collections;
using Awaken.Utility.Graphics;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace Awaken.ECS.Systems {
    [CreateAfter(typeof(BeginPresentationEntityCommandBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class ShadowsByVolume : SystemBase {
        EntityQuery _toUpdateQuery;
        EntityQuery _toFullResetQuery;
        EntityQuery _toResetQuery;
        EntityQuery _resetRequestQuery;

        ComponentTypeHandle<WorldRenderBounds> _worldRendersBoundsHandle;
        ComponentTypeHandle<LODRange> _lodRangesHandle;
        EntityTypeHandle _entityTypeHandle;

        BeginPresentationEntityCommandBufferSystem.Singleton _presentationEcbSingleton;

        UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum> _shadowVolumeData;

        protected override void OnCreate() {
            _worldRendersBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(true);
            _lodRangesHandle = GetComponentTypeHandle<LODRange>(true);
            _entityTypeHandle = GetEntityTypeHandle();

            var queryBuilder = new EntityQueryBuilder(ARAlloc.Temp);
            _toUpdateQuery = queryBuilder
                .WithAll<RenderFilterSettings, WorldRenderBounds, LODRange>()
                .WithAbsent<ShadowsProcessedTag>()
                .Build(this);
            queryBuilder.Dispose();

            _toFullResetQuery = GetEntityQuery(ComponentType.ReadWrite<ShadowsProcessedTag>(), ComponentType.ReadWrite<ShadowsChangedTag>(), ComponentType.ReadWrite<RenderFilterSettings>());
            _toResetQuery = GetEntityQuery(ComponentType.ReadWrite<ShadowsProcessedTag>());

            _resetRequestQuery = GetEntityQuery(ComponentType.ReadWrite<ResetShadowsRequestTag>());

            RequireAnyForUpdate(_toUpdateQuery, _resetRequestQuery);

            _presentationEcbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();

            _shadowVolumeData = SmallObjectsShadows.LoadData(ShadowsConfigChanged);
        }

        protected override void OnDestroy() {
            SmallObjectsShadows.ReleaseData(ShadowsConfigChanged);
        }

        protected override void OnUpdate() {
            RevertShadowChangesIfNeeded();

            _worldRendersBoundsHandle.Update(this);
            _lodRangesHandle.Update(this);
            _entityTypeHandle.Update(this);

            EntityManager.GetAllUniqueSharedComponents<RenderFilterSettings>(out var renderingFilterSettings, ARAlloc.Temp);
            var jobHandles = new NativeList<JobHandle>(renderingFilterSettings.Length, ARAlloc.Temp);

            foreach (var renderingFilterSetting in renderingFilterSettings) {
                var parallelWriter = _presentationEcbSingleton.CreateCommandBuffer(World.Unmanaged).AsParallelWriter();
                if (renderingFilterSetting.ShadowCastingMode != ShadowCastingMode.On) {
                    var entities = _toUpdateQuery.ToEntityArray(ARAlloc.Temp);
                    parallelWriter.AddComponent<ShadowsProcessedTag>(0, entities);
                    entities.Dispose();
                } else {
                    _toUpdateQuery.SetSharedComponentFilter(renderingFilterSetting);
                    var jobHandle = new UpdateShadowsJob {
                        filterSettings = renderingFilterSetting,
                        rendererBoundsHandle = _worldRendersBoundsHandle,
                        lodRangesHandle = _lodRangesHandle,
                        entityTypeHandle = _entityTypeHandle,
                        shadowVolumeData = _shadowVolumeData,

                        ecb = parallelWriter,
                    }.Schedule(_toUpdateQuery, Dependency);
                    jobHandles.Add(jobHandle);
                }
            }

            Dependency = JobHandle.CombineDependencies(jobHandles.AsArray());

            renderingFilterSettings.Dispose();
            jobHandles.Dispose();
        }

        void RevertShadowChangesIfNeeded() {
            var resetRequestCount = _resetRequestQuery.CalculateChunkCount();
            if (resetRequestCount > 0) {
                // Restore shadow casting mode
                var entities = _toFullResetQuery.ToEntityArray(ARAlloc.Temp);
                foreach (var entity in entities) {
                    var newFilterSettings = EntityManager.GetSharedComponent<RenderFilterSettings>(entity);
                    newFilterSettings.ShadowCastingMode = ShadowCastingMode.On;
                    EntityManager.SetSharedComponent(entity, newFilterSettings);
                }
                entities.Dispose();
                // Remove tags
                var fullComponentTypeSet = new ComponentTypeSet(ComponentType.ReadWrite<ShadowsProcessedTag>(), ComponentType.ReadWrite<ShadowsChangedTag>());
                EntityManager.RemoveComponent(_toFullResetQuery, fullComponentTypeSet);
                var processedComponentTypeSet = new ComponentTypeSet(ComponentType.ReadWrite<ShadowsProcessedTag>());
                EntityManager.RemoveComponent(_toResetQuery, processedComponentTypeSet);
                // Clear request
                EntityManager.RemoveComponent<ResetShadowsRequestTag>(_resetRequestQuery);
            }
        }

        void ShadowsConfigChanged(in UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum> newData) {
            Dependency.Complete();
            _shadowVolumeData = newData;

            EntityManager.CreateEntity(ComponentType.ReadWrite<ResetShadowsRequestTag>());
        }

        [BurstCompile]
        struct UpdateShadowsJob : IJobChunk {
            public RenderFilterSettings filterSettings;
            [ReadOnly] public ComponentTypeHandle<WorldRenderBounds> rendererBoundsHandle;
            [ReadOnly] public ComponentTypeHandle<LODRange> lodRangesHandle;
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            [ReadOnly] public UnsafeArray<SmallObjectsShadows.ShadowVolumeDatum>.Span shadowVolumeData;

            public EntityCommandBuffer.ParallelWriter ecb;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                var renderersBounds = chunk.GetNativeArray(ref rendererBoundsHandle);
                var lodRanges = chunk.GetNativeArray(ref lodRangesHandle);
                var entities = chunk.GetNativeArray(entityTypeHandle);

                var changedEntities = new NativeList<Entity>(entities.Length, ARAlloc.Temp);

                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++) {
                    var rendererBounds = renderersBounds[i];
                    var lodRange = lodRanges[i];
                    if (SmallObjectsShadows.ShouldDisableShadows(lodRange, rendererBounds.Value, shadowVolumeData)) {
                        changedEntities.Add(entities[i]);
                    }
                }

                if (changedEntities.Length > 0) {
                    var entitiesArray = changedEntities.AsArray();
                    var changedFilterSettings = filterSettings;
                    changedFilterSettings.ShadowCastingMode = ShadowCastingMode.Off;
                    ecb.SetSharedComponent(unfilteredChunkIndex, entitiesArray, changedFilterSettings);
                    ecb.AddComponent<ShadowsChangedTag>(unfilteredChunkIndex, entitiesArray);
                }
                ecb.AddComponent<ShadowsProcessedTag>(unfilteredChunkIndex, entities);

                changedEntities.Dispose();
            }
        }
    }
}
