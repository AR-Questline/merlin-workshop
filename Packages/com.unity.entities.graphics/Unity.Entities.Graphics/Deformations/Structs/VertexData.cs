using Unity.Mathematics;

namespace Unity.Rendering
{
    /// <summary>
    /// Represent vertex data for a SharedMesh buffer
    /// </summary>
    /// <remarks>
    /// This must map between compute shaders and CPU data.
    /// </remarks>
    internal struct VertexData
    {
        public float3 Position;
        public float3 Normal;
        public float3 Tangent;
    }
}
