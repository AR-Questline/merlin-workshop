#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Mathematics;

namespace Unity.Rendering.Occlusion.Masked
{
    internal struct ClippedOccluder
    {
        public int sourceIndexOffset;
        public float4 screenMin, screenMax;
        
        // If a triangle intersects the frustum, it needs to be clipped. The process of clipping converts the triangle
        // into a non-triangular polygon with more vertices. Up to 5 new vertices can be added in this process,
        // depending on how the triangle intersects the frustum. We allocate a large enough vertex buffer to be able to
        // fit all the vertices, and we track how many vertices are actually generated after clipping in this variable.
        public int expandedVertexSize;
    }
}

#endif
