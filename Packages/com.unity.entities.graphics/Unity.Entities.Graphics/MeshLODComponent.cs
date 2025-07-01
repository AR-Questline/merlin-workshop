using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    /// <summary>
    /// Represents an LOD group.
    /// </summary>
    /// <remarks>
    /// Each MeshLODGroupComponent contains multiple MeshLODComponents and can also have multiple child groups.
    /// </remarks>
    public struct MeshLODGroupComponent : IComponentData
    {
        /// <summary>
        /// The LOD distances for the four closest LODS.
        /// </summary>
        public float4    LODDistances0;

        /// <summary>
        /// The LOD distances for the four furthest LODS.
        /// </summary>
        public float4    LODDistances1;

        /// <summary>
        /// The local reference point which Entities Graphics uses to calculate the distance from the camera to the LOD group.
        /// </summary>
        public float3    LocalReferencePoint;
    }

    /// <summary>
    /// An unmanaged component that represents a world reference point to use for LOD group.
    /// </summary>
    public struct LODGroupWorldReferencePoint : IComponentData
    {
        /// <summary>
        /// The world-space x, y, and z position of the reference point.
        /// </summary>
        public float3 Value;
    }

    internal struct SkipLODGroupWorldReferencePointUpdate : IComponentData
    {

    }

    /// <summary>
    /// An unamanged component that represents a mesh LOD entity.
    /// </summary>
    public struct MeshLODComponent : IComponentData
    {
        /// <summary>
        /// The LOD group entity.
        /// </summary>
        public Entity   Group;

        /// <summary>
        /// The LOD mask.
        /// </summary>
        public int      LODMask;
    }
}
