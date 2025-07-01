namespace Unity.Rendering
{
    /// <summary>
    /// Represents per-thread statistics that Entities Graphics collects during runtime.
    /// </summary>
    /// <remarks>
    /// Per-thread statistics are only available in the Unity Editor. Unity strips this functionality from standalone player builds so it doesn't affect performance.
    /// </remarks>
    public struct EntitiesGraphicsPerThreadStats
    {
        // This struct needs to be padded to a cache line multiple to avoid false sharing
        // If you add or remove any members here you must add or remove padding to the struct

        /// <summary>
        /// The number of chunks that Entities Graphics considered for frustum culling.
        /// </summary>
        /// <remarks>        
        /// This value includes any chunks successfully added into a batch, regardless of whether they contained any visible entities.
        /// </remarks>
        public int ChunkTotal;

        /// <summary>
        /// The number of chunks that contained at least one entity that passed LOD selection.
        /// </summary>
        /// <remarks>
        /// This value can include chunks that are invisible. Frustum culling happens after LOD selection and it can discard chunks that pass LOD selection.
        /// </remarks>
        public int ChunkCountAnyLod;

        /// <summary>
        /// The number of chunks partially culled by the frustum.
        /// </summary>
        /// <remarks>
        /// Entities Graphics considers a chunk to be partially culled if the chunk's bounds are partially within the frustum.
        /// If this is the case, Entities Graphics must perform frustum culling for each entity in the chunk separately.
        /// If the bounds of a chunk are fully within the frustum, Entities Graphics knows all entities in the chunk are within the frustum and doesn't need to frustum cull them individually.
        /// </remarks>
        public int ChunkCountInstancesProcessed;

        /// <summary>
        /// The number of chunks that contained entities which are all in the frustum.
        /// </summary>
        public int ChunkCountFullyIn;

        /// <summary>
        /// The number of culling tests performed on partially culled chunks.
        /// </summary>
        public int InstanceTests;

        /// <summary>
        /// The number of chunks that Entities Graphics considered for LOD selection.
        /// </summary>
        public int LodTotal;

        /// <summary>
        /// The number of the chunks that contained entities without any LOD data.
        /// </summary>
        public int LodNoRequirements;

        /// <summary>
        /// The number of chunks that contained at least one LOD change.
        /// </summary>
        /// <remarks>        
        /// An LOD change means that an entity that represented an LOD was either enabled or disabled depending on the LOD selection.
        /// </remarks>
        public int LodChanged;

        /// <summary>
        /// The number of chunks that Entities Graphics recalculated LOD selection for.
        /// </summary>
        /// <remarks>
        /// If the scaled distance from the camera changes enough to possibly cause a LOD change to entities in a chunk, Entities Graphics recalculates LOD selection for the chunk.
        /// </remarks>
        public int LodChunksTested;

        /// <summary>
        /// The number of entities that Entities Graphics rendered into at least one rendering pass.
        /// </summary>
        /// <remarks>
        /// This doesn't perfectly represent the total number of times that Entities Graphics renders entities. 
        /// If Entities Graphics renders an entity multiple times, for example if it uses the same culling result to draw the entity into several rendering passes, this value only counts the entity once. This commonly happens if Entities Graphics draws the entity both into a depth prepass and into a G-Buffer or forward pass.
        /// If Entities Graphics performs culling for the same entity multiple times, for example if the entity is visible in both a camera and the directional shadow map, this value counts the entity multiple times. 
        /// </remarks>
        public int RenderedEntityCount;

        /// <summary>
        /// The number of the draw commands.
        /// </summary>
        public int DrawCommandCount;

        /// <summary>
        /// The number of the ranges.
        /// </summary>
        public int DrawRangeCount;
    }

    /// <summary>
    /// Represents statistics that Entities Graphics collects during runtime.
    /// </summary>
    public struct EntitiesGraphicsStats
    {
        /// <summary>
        /// Total number of chunks
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int ChunkTotal;

        /// <summary>
        /// Total number of chunks if any of the LOD are enabled in ChunkInstanceLodEnabled entity.
        /// </summary>
        public int ChunkCountAnyLod;

        /// <summary>
        /// Total number of chunks processed which are partially intersected with the view frustum.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int ChunkCountInstancesProcessed;

        /// <summary>
        /// Total number of chunks processed which are fully inside of the view frustum.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int ChunkCountFullyIn;

        /// <summary>
        /// Total number of instance checks across all LODs.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int InstanceTests;

        /// <summary>
        /// Total number of LODs across all archetype entities chunks.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int LodTotal;

        /// <summary>
        /// Number of the culling chunks without LOD data.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int LodNoRequirements;

        /// <summary>
        /// Number of enabled or disabled LODs in this frame.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int LodChanged;

        /// <summary>
        /// Number of tested LOD chunks.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int LodChunksTested;

        /// <summary>
        /// Camera move distance since the last frame.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public float CameraMoveDistance;

        /// <summary>
        /// Total number of batches.
        /// </summary>
        public int BatchCount;

        /// <summary>
        /// Accumulated number of rendered entities across all threads.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int RenderedInstanceCount;

        /// <summary>
        /// Accumulated number of the draw commands.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int DrawCommandCount;

        /// <summary>
        /// Accumulated number of the draw ranges.
        /// </summary>
        /// <remarks>        
        /// This stat is only available in the Editor.
        /// </remarks>
        public int DrawRangeCount;

        /// <summary>
        /// The total number of bytes of the GPU memory used including upload and fence memory.
        /// </summary>
        public long BytesGPUMemoryUsed;

        /// <summary>
        /// The number of bytes of GPU memory that Entities Graphics used to update changed entity data.
        /// </summary>
        /// <remarks>
        /// For example, Entities Graphics must update the transform for entities that move, and the amount of data required to changed transforms contributes to this memory.
        /// This helps you understand how much entity data changes over time.
        /// </remarks>
        public long BytesGPUMemoryUploadedCurr;

        /// <summary>
        /// The maximum number of bytes of GPU memory that Entities Graphics used to update changed entity data.
        /// </summary>
        public long BytesGPUMemoryUploadedMax;
    }
}
