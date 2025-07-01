using System;
using System.Diagnostics;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Rendering
{
    /// <summary>
    /// A tag component that allows for granular per-instance culling control.
    /// </summary>
    public struct PerInstanceCullingTag : IComponentData {}

    public struct LODWorldReferencePoint : IComponentData
    {
        public float3 Value;
    }

    public struct SkipLODWorldReferencePointUpdate : IComponentData
    {
    }

    public struct LODRange : IComponentData
    {
        public float MinDist;
        public float MaxDist;
        public int LODMask;

        public LODRange(MeshLODGroupComponent lodGroup, int lodMask)
        {
            float minDist = float.MaxValue;
            float maxDist = 0.0F;

            if ((lodMask & 0x01) == 0x01)
            {
                minDist = 0.0f;
                maxDist = math.max(maxDist, lodGroup.LODDistances0.x);
            }
            if ((lodMask & 0x02) == 0x02)
            {
                minDist = math.min(minDist, lodGroup.LODDistances0.x);
                maxDist = math.max(maxDist, lodGroup.LODDistances0.y);
            }
            if ((lodMask & 0x04) == 0x04)
            {
                minDist = math.min(minDist, lodGroup.LODDistances0.y);
                maxDist = math.max(maxDist, lodGroup.LODDistances0.z);
            }
            if ((lodMask & 0x08) == 0x08)
            {
                minDist = math.min(minDist, lodGroup.LODDistances0.z);
                maxDist = math.max(maxDist, lodGroup.LODDistances0.w);
            }
            if ((lodMask & 0x10) == 0x10)
            {
                minDist = math.min(minDist, lodGroup.LODDistances0.w);
                maxDist = math.max(maxDist, lodGroup.LODDistances1.x);
            }
            if ((lodMask & 0x20) == 0x20)
            {
                minDist = math.min(minDist, lodGroup.LODDistances1.x);
                maxDist = math.max(maxDist, lodGroup.LODDistances1.y);
            }
            if ((lodMask & 0x40) == 0x40)
            {
                minDist = math.min(minDist, lodGroup.LODDistances1.y);
                maxDist = math.max(maxDist, lodGroup.LODDistances1.z);
            }
            if ((lodMask & 0x80) == 0x80)
            {
                minDist = math.min(minDist, lodGroup.LODDistances1.z);
                maxDist = math.max(maxDist, lodGroup.LODDistances1.w);
            }

            MinDist = minDist;
            MaxDist = maxDist;
            LODMask = lodMask;
        }
        
        public static int GetLODIndex(int lodMask) {
            return math.tzcnt(lodMask);
        }
    }

    public struct SkipLODRangeUpdate : IComponentData
    {
    }

    [UpdateInGroup(typeof(StructuralChangePresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations | WorldSystemFilterFlags.Editor)]
    internal partial class AddLODRequirementComponents : SystemBase
    {
        EntityQuery m_MissingLODRange;
        EntityQuery m_MissingLODWorldReferencePoint;
        EntityQuery m_MissingLODGroupWorldReferencePoint;

        /// <summary>
        /// Called when this system is created.
        /// </summary>
        protected override void OnCreate()
        {
            m_MissingLODRange = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<MeshLODComponent>() },
                None = new[] { ComponentType.ReadOnly<LODRange>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });

            m_MissingLODWorldReferencePoint = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<MeshLODComponent>() },
                None = new[] { ComponentType.ReadOnly<LODWorldReferencePoint>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });

            m_MissingLODGroupWorldReferencePoint = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<MeshLODGroupComponent>() },
                None = new[] { ComponentType.ReadOnly<LODGroupWorldReferencePoint>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
        }

        /// <summary>
        /// Called when this system is updated.
        /// </summary>
        protected override void  OnUpdate()
        {
            EntityManager.AddComponent(m_MissingLODRange, typeof(LODRange));
            EntityManager.AddComponent(m_MissingLODWorldReferencePoint, typeof(LODWorldReferencePoint));
            EntityManager.AddComponent(m_MissingLODGroupWorldReferencePoint, typeof(LODGroupWorldReferencePoint));
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations | WorldSystemFilterFlags.Editor)]
    internal partial class LODRequirementsUpdateSystem : SystemBase
    {
        EntityQuery m_UpdatedLODRanges;
        EntityQuery m_LODReferencePoints;
        EntityQuery m_LODGroupReferencePoints;

        ComponentLookup<MeshLODGroupComponent> MeshLODGroupComponent;
        ComponentTypeHandle<MeshLODComponent> MeshLODComponent;
        ComponentLookup<LocalToWorld> LocalToWorldLookup;
        ComponentTypeHandle<LODRange> LODRange;

        [BurstCompile]
        public struct UpdateLODRangesJob : IJobChunk
        {
            [ReadOnly] public ComponentLookup<MeshLODGroupComponent>    MeshLODGroupComponent;
            [ReadOnly] public ComponentTypeHandle<MeshLODComponent>     MeshLODComponent;
            public ComponentTypeHandle<LODRange>                        LODRange;

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void CheckDeepHLODSupport(Entity entity)
            {
                if (entity != Entity.Null)
                    throw new System.NotImplementedException("Deep HLOD is not supported yet");
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var lodRange = chunk.GetNativeArray(ref LODRange);
                var meshLods = chunk.GetNativeArray(ref MeshLODComponent);
                var instanceCount = chunk.Count;

                for (int i = 0; i < instanceCount; i++)
                {
                    var meshLod = meshLods[i];
                    var lodGroupEntity = meshLod.Group;
                    if (Hint.Likely(MeshLODGroupComponent.TryGetComponent(lodGroupEntity, out var lodGroup))) {
                        var lodMask = meshLod.LODMask;
                        lodRange[i] = new LODRange(lodGroup, lodMask);
                    }
#if UNITY_EDITOR
                    else {
                        throw new Exception("[UpdateLODRangesJob] MeshLODGroupComponent not found on LODGroupEntity");
                    }
#endif
                }
            }
        }

        [BurstCompile]
        internal struct UpdateLODGroupWorldReferencePointsJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<MeshLODGroupComponent> MeshLODGroupComponent;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorld;
            public ComponentTypeHandle<LODGroupWorldReferencePoint> LODGroupWorldReferencePoint;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var meshLODGroupComponent = chunk.GetNativeArray(ref MeshLODGroupComponent);
                var localToWorld = chunk.GetNativeArray(ref LocalToWorld);
                var lodGroupWorldReferencePoint = chunk.GetNativeArray(ref LODGroupWorldReferencePoint);
                var instanceCount = chunk.Count;

                for (int i = 0; i < instanceCount; i++)
                {
                    lodGroupWorldReferencePoint[i] = new LODGroupWorldReferencePoint { Value = math.transform(localToWorld[i].Value, meshLODGroupComponent[i].LocalReferencePoint) };
                }
            }
        }

        [BurstCompile]
        internal struct UpdateLODWorldReferencePointsJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<MeshLODComponent> MeshLODComponent;
            [ReadOnly] public ComponentLookup<LODGroupWorldReferencePoint> LODGroupWorldReferencePoint;
            public ComponentTypeHandle<LODWorldReferencePoint> LODWorldReferencePoint;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var lodWorldReferencePoint = chunk.GetNativeArray(ref LODWorldReferencePoint);
                var meshLods = chunk.GetNativeArray(ref MeshLODComponent);
                var instanceCount = chunk.Count;

                for (int i = 0; i < instanceCount; i++)
                {
                    var meshLod = meshLods[i];
                    var lodGroupEntity = meshLod.Group;
                    if (Hint.Likely(LODGroupWorldReferencePoint.TryGetComponent(lodGroupEntity, out var lodGroupWorldReferencePoint))) {
                         lodWorldReferencePoint[i] = new LODWorldReferencePoint { Value = lodGroupWorldReferencePoint.Value };
                    }
#if UNITY_EDITOR
                    else {
                        throw new Exception("[UpdateLODWorldReferencePointsJob] LODGroupWorldReferencePoint not found on LODGroupEntity");
                    }
#endif
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            // Change filter: LODGroupConversion add MeshLODComponent for all LOD children. When the MeshLODComponent is added/changed, we recalculate LOD ranges.
            m_UpdatedLODRanges = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<LocalToWorld>(), typeof(MeshLODComponent), typeof(LODRange)
                },
                None = new ComponentType[] { typeof(SkipLODRangeUpdate) }
            });
            m_UpdatedLODRanges.SetChangedVersionFilter(ComponentType.ReadWrite<MeshLODComponent>());

            m_LODReferencePoints = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadOnly<MeshLODComponent>(), typeof(LODWorldReferencePoint)
                },
                None = new ComponentType[] { typeof(SkipLODWorldReferencePointUpdate) }
            });

            // Change filter: LOD Group world reference points only change when MeshLODGroupComponent or LocalToWorld change
            m_LODGroupReferencePoints = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<MeshLODGroupComponent>(), ComponentType.ReadOnly<LocalToWorld>(),
                    typeof(LODGroupWorldReferencePoint)
                },
                None = new ComponentType[] { typeof(SkipLODGroupWorldReferencePointUpdate) }
            });
            m_LODGroupReferencePoints.SetChangedVersionFilter(new[] { ComponentType.ReadWrite<MeshLODGroupComponent>(), ComponentType.ReadWrite<LocalToWorld>() });

            MeshLODGroupComponent = GetComponentLookup<MeshLODGroupComponent>(true);
            MeshLODComponent = GetComponentTypeHandle<MeshLODComponent>(true);
            LocalToWorldLookup = GetComponentLookup<LocalToWorld>(true);
            LODRange = GetComponentTypeHandle<LODRange>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            MeshLODGroupComponent.Update(this);
            MeshLODComponent.Update(this);
            LocalToWorldLookup.Update(this);
            LODRange.Update(this);

            var updateLODRangesJob = new UpdateLODRangesJob
            {
                MeshLODGroupComponent = MeshLODGroupComponent,
                MeshLODComponent = MeshLODComponent,
                LODRange = LODRange
            };

            var updateGroupReferencePointJob = new UpdateLODGroupWorldReferencePointsJob
            {
                MeshLODGroupComponent = GetComponentTypeHandle<MeshLODGroupComponent>(true),
                LocalToWorld = GetComponentTypeHandle<LocalToWorld>(true),
                LODGroupWorldReferencePoint = GetComponentTypeHandle<LODGroupWorldReferencePoint>(),
            };

            var updateReferencePointJob = new UpdateLODWorldReferencePointsJob
            {
                MeshLODComponent = GetComponentTypeHandle<MeshLODComponent>(true),
                LODGroupWorldReferencePoint = GetComponentLookup<LODGroupWorldReferencePoint>(true),
                LODWorldReferencePoint = GetComponentTypeHandle<LODWorldReferencePoint>(),
            };

            var depLODRanges = updateLODRangesJob.ScheduleParallel(m_UpdatedLODRanges, Dependency);
            var depGroupReferencePoints = updateGroupReferencePointJob.ScheduleParallel(m_LODGroupReferencePoints, Dependency);
            var depCombined = JobHandle.CombineDependencies(depLODRanges, depGroupReferencePoints);
            Dependency = updateReferencePointJob.ScheduleParallel(m_LODReferencePoints, depCombined);
        }
    }
}
