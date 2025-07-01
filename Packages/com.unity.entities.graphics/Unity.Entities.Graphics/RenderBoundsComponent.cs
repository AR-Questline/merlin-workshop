using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    /// <summary>
    /// An unmanaged component that represent the render bounds.
    /// </summary>
    /// <remarks>
    /// This is a bounding box for the entity in local coordinates. You can enlarge this box in cases where vertex animations can move parts of the mesh outside the mesh's original bounds.
    /// </remarks>
    public struct RenderBounds : IComponentData
    {
        /// <summary>
        /// The axis-aligned render bounds.
        /// </summary>
        public AABB Value;
    }

    /// <summary>
    /// An unmanaged component that represents the world-space render bounds.
    /// </summary>
    /// <remarks>
    /// Entities Graphics automatically calculates this component value and uses it for visibility culling.
    /// </remarks>
    public struct WorldRenderBounds : IComponentData
    {
        /// <summary>
        /// The axis-aligned render bounds.
        /// </summary>
        public AABB Value;
    }

    internal struct SkipWorldRenderBoundsUpdate : IComponentData
    {

    }

    /// <summary>
    /// An unmanaged component that represents the render bounds of a chunk.
    /// </summary>
    /// <remarks>
    /// This is the combined world-space bounds for every entity inside the chunk. Entities Graphics uses it for visibility culling at the chunk level.
    /// </remarks>
    public struct ChunkWorldRenderBounds : IComponentData
    {
        /// <summary>
        /// The axis-aligned render bounds.
        /// </summary>
        public AABB Value;
    }
}
