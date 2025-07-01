using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
    /// <summary>
    /// Material property that contains the index where the mesh data (pos, nrm, tan) 
    /// of a deformed mesh starts in the deformed mesh instance buffer.
    /// The deformed mesh buffer is double buffered to keep previous vertex positions around for motion vectors.
    /// Use MeshBufferManager.ActiveDeformedMeshBufferIndex to access the DeformedMeshIndex for the current frame.
    /// x,y = position in current and previous frame buffers
    /// z = the current frame index (used as index into x and y properties)
    /// w = unused
    /// </summary>
    /// This should be split into two separate components (GFXMESH-79)
    [MaterialProperty("_DotsDeformationParams")]
    internal struct DeformedMeshIndex : IComponentData
    {
        public uint4 Value;
    }
#else
    [MaterialProperty("_ComputeMeshIndex")]
    internal struct DeformedMeshIndex : IComponentData
    {
        public uint Value;
    }
#endif

    /// <summary>
    /// Used by render entities to retrieve the deformed entity which
    /// holds the animated data that controls the mesh deformation,
    /// such as skin matrices or blend shape weights.
    /// </summary>
    internal struct DeformedEntity : IComponentData
    {
        public Entity Value;
    }
}
