using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Rendering
{
    /// <summary>
    /// A system that generates a scene bounding volume for each entity scene section at conversion time.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.EntitySceneOptimizations)]
    [UpdateAfter(typeof(RenderBoundsUpdateSystem))]
    partial class UpdateSceneBoundingVolumeFromRendererBounds : SystemBase
    {
        [BurstCompile]
        struct CollectSceneBoundsJob : IJob
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<WorldRenderBounds> RenderBounds;

            public Entity SceneBoundsEntity;
            public ComponentLookup<Unity.Scenes.SceneBoundingVolume> SceneBounds;

            public void Execute()
            {
                var minMaxAabb = MinMaxAABB.Empty;
                for (int i = 0; i != RenderBounds.Length; i++)
                {
                    var aabb = RenderBounds[i].Value;

                    // MUST BE FIXED BY DOTS-2518
                    //
                    // Avoid empty RenderBounds AABB because is means it hasn't been computed yet
                    // There are some unfortunate cases where RenderBoundsUpdateSystem is executed after this system
                    //  and a bad Scene AABB is computed if we consider these empty RenderBounds AABB.
                    if (math.lengthsq(aabb.Center) != 0.0f && math.lengthsq(aabb.Extents) != 0.0f)
                    {
                        minMaxAabb.Encapsulate(aabb);
                    }
                }
                SceneBounds[SceneBoundsEntity] = new Unity.Scenes.SceneBoundingVolume { Value = minMaxAabb };
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            //@TODO: API does not allow me to use ChunkComponentData.
            //Review with simon how we can improve it.

            var query = GetEntityQuery(typeof(WorldRenderBounds), typeof(SceneSection));

            EntityManager.GetAllUniqueSharedComponents<SceneSection>(out var sections, Allocator.Temp);
            foreach (var section in sections)
            {
                if (section.Equals(default(SceneSection)))
                    continue;

                query.SetSharedComponentFilter(section);

                var entity = EntityManager.CreateEntity(typeof(Unity.Scenes.SceneBoundingVolume));
                EntityManager.AddSharedComponent(entity, section);

                var job = new CollectSceneBoundsJob();
                job.RenderBounds = query.ToComponentDataArray<WorldRenderBounds>(Allocator.TempJob);
                job.SceneBoundsEntity = entity;
                job.SceneBounds = GetComponentLookup<Unity.Scenes.SceneBoundingVolume>();
                job.Run();
            }

            query.ResetFilter();
        }
    }


    [UpdateInGroup(typeof(StructuralChangePresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations | WorldSystemFilterFlags.Editor)]
    partial class AddWorldAndChunkRenderBounds : SystemBase
    {
        EntityQuery m_MissingWorldRenderBounds;
        EntityQuery m_MissingWorldChunkRenderBounds;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_MissingWorldRenderBounds = GetEntityQuery
                (
                new EntityQueryDesc
                {
                    All = new[] {ComponentType.ReadOnly<RenderBounds>(), ComponentType.ReadOnly<LocalToWorld>()},
                    None = new[] {ComponentType.ReadOnly<WorldRenderBounds>()},
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
                }
                );

            m_MissingWorldChunkRenderBounds = GetEntityQuery
                (
                new EntityQueryDesc
                {
                    All = new[] {ComponentType.ReadOnly<RenderBounds>(), ComponentType.ReadOnly<LocalToWorld>()},
                    None = new[] { ComponentType.ChunkComponentReadOnly<ChunkWorldRenderBounds>() },
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
                }
                );
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_MissingWorldRenderBounds, ComponentType.ReadWrite<WorldRenderBounds>());
            EntityManager.AddComponent(m_MissingWorldChunkRenderBounds, ComponentType.ChunkComponent<ChunkWorldRenderBounds>());
        }
    }

    /// <summary>
    /// A system that updates the WorldRenderBounds for entities that have both a LocalToWorld and RenderBounds component.
    /// </summary>
    /// <remarks>
    /// This system also ensures that a WorldRenderBounds exists on entities that have a LocalToWorld and RenderBounds component.
    /// </remarks>
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations | WorldSystemFilterFlags.Editor)]
    internal partial class RenderBoundsUpdateSystem : SystemBase
    {
        EntityQuery m_RenderBounds;
        EntityQuery m_WorldRenderBounds;

        [BurstCompile]
        internal struct BoundsJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<RenderBounds> RendererBounds;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorld;
            public ComponentTypeHandle<WorldRenderBounds> WorldRenderBounds;
            public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBounds;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var worldBounds = chunk.GetNativeArray(ref WorldRenderBounds);
                var localBounds = chunk.GetNativeArray(ref RendererBounds);
                var localToWorld = chunk.GetNativeArray(ref LocalToWorld);
                MinMaxAABB combined = MinMaxAABB.Empty;
                for (int i = 0; i != localBounds.Length; i++)
                {
                    var transformed = AABB.Transform(localToWorld[i].Value, localBounds[i].Value);

                    worldBounds[i] = new WorldRenderBounds { Value = transformed };
                    combined.Encapsulate(transformed);
                }

                chunk.SetChunkComponentData(ref ChunkWorldRenderBounds, new ChunkWorldRenderBounds { Value = combined });
            }
        }

        [BurstCompile]
        internal struct WorldBoundsJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<WorldRenderBounds> WorldRenderBounds;
            public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBounds;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var worldBounds = chunk.GetNativeArray(ref WorldRenderBounds);
                MinMaxAABB combined = MinMaxAABB.Empty;
                for (int i = 0; i != worldBounds.Length; i++)
                {
                    combined.Encapsulate(worldBounds[i].Value);
                }

                chunk.SetChunkComponentData(ref ChunkWorldRenderBounds, new ChunkWorldRenderBounds { Value = combined });
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            if (!EntitiesGraphicsSystem.EntitiesGraphicsEnabled)
            {
                Enabled = false;
                return;
            }

            m_RenderBounds = GetEntityQuery
                (
                new EntityQueryDesc
                {
                    All = new[] { ComponentType.ChunkComponent<ChunkWorldRenderBounds>(), ComponentType.ReadWrite<WorldRenderBounds>(), ComponentType.ReadOnly<RenderBounds>(), ComponentType.ReadOnly<LocalToWorld>() },
                    None = new[] { ComponentType.ReadOnly<SkipWorldRenderBoundsUpdate>() }
                }
                );
            m_RenderBounds.SetChangedVersionFilter(new[] { ComponentType.ReadOnly<RenderBounds>(), ComponentType.ReadOnly<LocalToWorld>()});
            m_RenderBounds.AddOrderVersionFilter();

            m_WorldRenderBounds = GetEntityQuery
                (
                new EntityQueryDesc
                {
                    All = new[] { ComponentType.ChunkComponent<ChunkWorldRenderBounds>(), ComponentType.ReadOnly<WorldRenderBounds>() },
                    None = new[] { ComponentType.ReadOnly<SkipWorldRenderBoundsUpdate>() },
                    Absent = new [] { ComponentType.ReadOnly<RenderBounds>() }
                }
                );
            m_WorldRenderBounds.SetChangedVersionFilter(new[] { ComponentType.ReadOnly<WorldRenderBounds>() });
            m_WorldRenderBounds.AddOrderVersionFilter();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var chunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>();
            var boundsJob = new BoundsJob
            {
                RendererBounds = GetComponentTypeHandle<RenderBounds>(true),
                LocalToWorld = GetComponentTypeHandle<LocalToWorld>(true),
                WorldRenderBounds = GetComponentTypeHandle<WorldRenderBounds>(),
                ChunkWorldRenderBounds = chunkWorldRenderBoundsHandle,
            };
            Dependency = boundsJob.ScheduleParallel(m_RenderBounds, Dependency);

            var worldBoundsJob = new WorldBoundsJob
            {
                WorldRenderBounds = GetComponentTypeHandle<WorldRenderBounds>(true),
                ChunkWorldRenderBounds = chunkWorldRenderBoundsHandle,
            };
            Dependency = worldBoundsJob.ScheduleParallel(m_WorldRenderBounds, Dependency);
        }

#if false
        public void DrawGizmos()
        {
            var boundsQuery = GetEntityQuery(typeof(LocalToWorld), typeof(WorldRenderBounds), typeof(RenderBounds));
            var localToWorlds = boundsQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
            var worldBounds = boundsQuery.ToComponentDataArray<WorldRenderBounds>(Allocator.TempJob);
            var localBounds = boundsQuery.ToComponentDataArray<RenderBounds>(Allocator.TempJob);

            var chunkBoundsQuery = GetEntityQuery(ComponentType.ReadOnly<ChunkWorldRenderBounds>(), typeof(ChunkHeader));
            var chunksBounds = chunkBoundsQuery.ToComponentDataArray<ChunkWorldRenderBounds>(Allocator.TempJob);

            Gizmos.matrix = Matrix4x4.identity;

            // world bounds
            Gizmos.color = Color.green;
            for (int i = 0; i != worldBounds.Length; i++)
                Gizmos.DrawWireCube(worldBounds[i].Value.Center, worldBounds[i].Value.Size);

            // chunk world bounds
            Gizmos.color = Color.yellow;
            for (int i = 0; i != chunksBounds.Length; i++)
                Gizmos.DrawWireCube(chunksBounds[i].Value.Center, chunksBounds[i].Value.Size);

            // local render bounds
            Gizmos.color = Color.blue;
            for (int i = 0; i != localToWorlds.Length; i++)
            {
                Gizmos.matrix = new Matrix4x4(localToWorlds[i].Value.c0, localToWorlds[i].Value.c1, localToWorlds[i].Value.c2, localToWorlds[i].Value.c3);
                Gizmos.DrawWireCube(localBounds[i].Value.Center, localBounds[i].Value.Size);
            }

            localToWorlds.Dispose();
            worldBounds.Dispose();
            localBounds.Dispose();
            chunksBounds.Dispose();
        }

        //@TODO: We really need a system level gizmo callback.
        [UnityEditor.DrawGizmo(UnityEditor.GizmoType.NonSelected)]
        public static void DrawGizmos(Light light, UnityEditor.GizmoType type)
        {
            if (light.type == LightType.Directional && light.isActiveAndEnabled)
            {
                if (World.DefaultGameObjectInjectionWorld == null)
                    return;

                var renderer = World.DefaultGameObjectInjectionWorld.GetExistingSystem<RenderBoundsUpdateSystem>();
                if (renderer != null)
                    renderer.DrawGizmos();
            }
        }

#endif
    }
}
