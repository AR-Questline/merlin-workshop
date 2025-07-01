#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Hybrid.Baking;
using UnityEngine;

namespace Unity.Rendering.Occlusion.Masked.Dots
{
    // This is an empty tag component
    [BakingType]
    struct ProcessThisOccludee : IComponentData
    {
    }

    class OccludeeBaker : Baker<MeshRenderer>
    {
        public override void Bake(MeshRenderer authoring)
        {
            if (authoring.allowOcclusionWhenDynamic)
            {
                // Add the tag component, which is then picked up by our baking system
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ProcessThisOccludee());
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class AddChunkOcclusionTests : SystemBase
    {
        EntityQuery m_SoloQuery;
        EntityQuery m_ParentQuery;
        EntityQuery m_NoChunkQuery;
        EntityQuery m_RemovedOccludeesQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_SoloQuery = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<ProcessThisOccludee>(),
                        ComponentType.ReadOnly<RenderBounds>(),
                    },
                    None = new[] {ComponentType.ReadOnly<OcclusionTest>()},
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
                }
            );
            m_ParentQuery = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<ProcessThisOccludee>(),
                        ComponentType.ReadOnly<AdditionalEntitiesBakingData>(),
                    },
                    None = new[]
                    {
                        ComponentType.ReadOnly<OcclusionTest>(),
                        ComponentType.ReadOnly<RenderBounds>()
                    },
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
                }
            );
            m_NoChunkQuery = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new[] {ComponentType.ReadOnly<OcclusionTest>()},
                    None = new[] {ComponentType.ReadOnly<ChunkOcclusionTest>()},
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
                }
            );
            m_RemovedOccludeesQuery = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new[] {ComponentType.ReadOnly<OcclusionTest>()},
                    None = new[] {ComponentType.ReadOnly<ProcessThisOccludee>()},
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
                }
            );
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            // By default, the `OcclusionTest` component is not cleaned up live from the entity if you remove the
            // `Occludee` monobehavior from the GameObject in the subscene.
            // This is why we've made `ProcessThisOccludee` a `[BakingType]` so that it isn't removed automatically in a
            // Bake pass, but kept alive in the Baking-only world.
            //
            // Now we use this persistant tag to identify removal. This works because Bakers will automatically remove
            // any components they add when the MonoBehaviour for the Baker is removed.
            {
                var entities = m_RemovedOccludeesQuery.ToEntityArray(Allocator.Temp);
                EntityManager.RemoveComponent<OcclusionTest>(entities);
                for (int i = 0; i < entities.Length; i++)
                {
                    EntityManager.RemoveChunkComponent<ChunkOcclusionTest>(entities[i]);
                }
                entities.Dispose();
            }

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Solo entities have render bounds. To these entities, we attach the chunk component. No other processing
            // is needed.
            {
                var solos = m_SoloQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < solos.Length; i++)
                {
                    ecb.AddComponent(solos[i], new OcclusionTest(true));
                }
                solos.Dispose();
            }

            // Parent entities don't have render bounds, but during DOTS baking they might have spawned additional
            // "child" entities. This can happen when the occludees have sub-meshes, In which case each sub-mesh spawns
            // its own entity.
            // We need to iterate through all of these child entities and add chunk components to them, if they also
            // have render bounds.
            var parents = m_ParentQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < parents.Length; i++)
            {
                var children  = EntityManager.GetBuffer<AdditionalEntitiesBakingData>(parents[i]);
                for (int j = 0; j < children.Length; j++)
                {
                    var child = children[j].Value;
                    if (SystemAPI.HasComponent<RenderBounds>(child))
                    {
                        ecb.AddComponent(child, new OcclusionTest(true));
                    }
                }
            }
            parents.Dispose();

            ecb.Playback(EntityManager);
            ecb.Dispose();

            // Now we have added the occludee component to both, solo and parent entities. Now we add chunk components
            // to all of them together.
            EntityManager.AddComponent(m_NoChunkQuery, ComponentType.ChunkComponent<ChunkOcclusionTest>());
        }
    }
}

#endif
