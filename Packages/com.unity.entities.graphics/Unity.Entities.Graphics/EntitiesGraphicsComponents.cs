using Unity.Entities;

namespace Unity.Rendering
{
    /// <summary>
    /// A chunk component that contains rendering information about a chunk.
    /// </summary>
    /// <remarks>
    /// Entities Graphics adds this chunk component to each chunk that it considers valid for rendering.
    /// </remarks>
    // TODO: Try to get this struct to be 64 bytes or less so it fits in a cache line?
    public struct EntitiesGraphicsChunkInfo : IComponentData
    {
        /// <summary>
        /// The index of the batch.
        /// </summary>
        internal int BatchIndex;

        /// <summary>
        /// Begin index for component type metadata in external arrays.
        /// </summary>
        internal int ChunkTypesBegin;

        /// <summary>
        /// End index for component type metadata in external arrays.
        /// </summary>
        internal int ChunkTypesEnd;

        /// <summary>
        /// Culling data of the chunk.
        /// </summary>
        internal EntitiesGraphicsChunkCullingData CullingData;

        /// <summary>
        /// Chunk is valid for processing.
        /// </summary>
        internal bool Valid;
    }

    /// <summary>
    /// Culling data of the chunk.
    /// </summary>
    internal unsafe struct EntitiesGraphicsChunkCullingData
    {
        /// <summary>
        /// This flag is set if the chunk has LOD data.
        /// </summary>
        public const int kFlagHasLodData = 1 << 0;

        /// <summary>
        /// This flag is set if the chunk shall be culled.
        /// </summary>
        public const int kFlagInstanceCulling = 1 << 1;

        /// <summary>
        /// This flag is set is the chunk has per object motion.
        /// </summary>
        public const int kFlagPerObjectMotion = 1 << 2;

        /// <summary>
        /// Chunk offset in the batch.
        /// </summary>
        public int ChunkOffsetInBatch;

        /// <summary>
        /// Movement grace distance.
        /// </summary>
        public ushort MovementGraceFixed16;

        /// <summary>
        /// Per chunk flags.
        /// </summary>
        public byte Flags;
        public byte ForceLowLODPrevious;
        // TODO: Remove InstanceLodEnableds, replace by just initializing VisibleInstances.
        public ChunkInstanceLodEnabled InstanceLodEnableds;
        public fixed ulong FlippedWinding[2];
    }

    /// <summary>
    /// An unmanaged component that separates entities into different batches.
    /// </summary>
    /// <remarks>
    /// Entities with different PartitionValues are never in the same Entities Graphics batch.
    /// This allows you to force entities into separate batches which can be useful for things like draw call sorting.
    /// Entities Graphics treats entities that have no PartitionValue as if they have a PartitionValue of 0.
    /// </remarks>
    public struct EntitiesGraphicsBatchPartition : ISharedComponentData
    {
        /// <summary>
        /// The partition ID that Entities Graphics uses to sort entities into batches.
        /// </summary>
        public ulong PartitionValue;
    }

    /// <summary>
    /// A tag component that enables the unity_WorldToObject material property.
    /// </summary>
    /// <remarks>
    /// unity_WorldToObject contains the world to local conversion matrix.
    /// </remarks>
    public struct WorldToLocal_Tag : IComponentData {}

    /// <summary>
    /// A tag component that enables depth sorting for the entity.
    /// </summary>
    public struct DepthSorted_Tag : IComponentData {}
}
