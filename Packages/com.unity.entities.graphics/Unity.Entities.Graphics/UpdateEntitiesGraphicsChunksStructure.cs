using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Rendering
{
    /// <summary>
    /// A system that renders all entities that contain both RenderMesh and LocalToWorld components.
    /// </summary>
    //@TODO: Updating always necessary due to empty component group. When Component group and archetype chunks are unified, [RequireMatchingQueriesForUpdate] can be added.
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(StructuralChangePresentationSystemGroup))]
    public partial class UpdateHybridChunksStructure : SystemBase
    {
        private EntityQuery m_MissingHybridChunkInfo;
        private EntityQuery m_DisabledRenderingQuery;
#if UNITY_EDITOR
        private EntityQuery m_HasHybridChunkInfo;
#endif

        /// <summary>
        /// Called when this system is created.
        /// </summary>
        protected override void OnCreate()
        {
            m_MissingHybridChunkInfo = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ChunkComponentReadOnly<ChunkWorldRenderBounds>(),
                    ComponentType.ReadOnly<WorldRenderBounds>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadOnly<MaterialMeshInfo>(),
                },
                None = new[]
                {
                    ComponentType.ChunkComponentReadOnly<EntitiesGraphicsChunkInfo>(),
                    ComponentType.ReadOnly<DisableRendering>(),
                },

                // TODO: Add chunk component to disabled entities and prefab entities to work around
                // the fragmentation issue where entities are not added to existing chunks with chunk
                // components. Remove this once chunk components don't affect archetype matching
                // on entity creation.
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab,
            });

            m_DisabledRenderingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<DisableRendering>(),
                },
            });

#if UNITY_EDITOR
            m_HasHybridChunkInfo = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ChunkComponentReadOnly<EntitiesGraphicsChunkInfo>(),
                },
            });
#endif
        }

        /// <summary>
        /// Called when this system is updated.
        /// </summary>
        protected override void OnUpdate()
        {
            UnityEngine.Profiling.Profiler.BeginSample("UpdateHybridChunksStructure");
            {
#if UNITY_EDITOR
                if (EntitiesGraphicsEditorTools.DebugSettings.RecreateAllBatches)
                {
                    Debug.Log("Recreating all batches");
                    EntityManager.RemoveChunkComponentData<EntitiesGraphicsChunkInfo>(m_HasHybridChunkInfo);
                }
#endif

                EntityManager.AddComponent(m_MissingHybridChunkInfo, ComponentType.ChunkComponent<EntitiesGraphicsChunkInfo>());
                EntityManager.RemoveChunkComponentData<EntitiesGraphicsChunkInfo>(m_DisabledRenderingQuery);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
