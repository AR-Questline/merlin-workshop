using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering
{
    internal class MeshBufferManager
    {
        const int k_ChunkSize = 2048;

        static readonly int k_MaxSize = (int)math.min(SystemInfo.maxGraphicsBufferSize / UnsafeUtility.SizeOf<VertexData>(), int.MaxValue);

#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
        ComputeDoubleBufferWrapper<VertexData> m_DeformedMeshData;
        static readonly int k_CurrentFrameBufferProperty = Shader.PropertyToID("_DeformedMeshData");
        static readonly int k_PreviousFrameBufferProperty = Shader.PropertyToID("_PreviousFrameDeformedMeshData");

        public int ActiveDeformedMeshBufferIndex => (m_DeformedMeshData != null) ? m_DeformedMeshData.ActiveBufferIndex : 0;
#else
        ComputeBufferWrapper<VertexData> m_DeformedMeshData;
#endif

        public MeshBufferManager()
        {
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
            m_DeformedMeshData = new ComputeDoubleBufferWrapper<VertexData>(k_CurrentFrameBufferProperty, k_PreviousFrameBufferProperty, k_ChunkSize);
#else
            m_DeformedMeshData = new ComputeBufferWrapper<VertexData>(Shader.PropertyToID("_DeformedMeshData"), k_ChunkSize);
#endif
            m_DeformedMeshData.PushDataToGlobal();
        }

        public void Dispose()
        {
            m_DeformedMeshData.Destroy();
        }

        public bool ResizeAndPushDeformMeshBuffersIfRequired(int requiredSize)
        {
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
            var buffer = m_DeformedMeshData.ActiveBuffer;
#else
            var buffer = m_DeformedMeshData;
#endif
            var size = buffer.BufferSize;

            if (size <= requiredSize || size - requiredSize > k_ChunkSize)
            {
                var newSize = ((requiredSize / k_ChunkSize) + 1) * k_ChunkSize;

                if (newSize > k_MaxSize)
                {
                    // Only inform users if the content requires a buffer that is too big.
                    if (requiredSize > k_MaxSize)
                        UnityEngine.Debug.LogWarning("The world contains too many deformed meshes to fit into a single GraphicsBuffer. Not all deformed meshes are guaranteed to render correctly. Reduce the number of active deformed meshes.");

                    // Do not actually resize the buffer if we are already at max capacity.
                    if (size == k_MaxSize)
                        return false;

                    newSize = k_MaxSize;
                }

                buffer.Resize(newSize);
                buffer.PushDataToGlobal();

                return true;
            }

            return false;
        }

#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
        public void FlipDeformedMeshBuffer()
        {
            m_DeformedMeshData.SetActiveBuffer(m_DeformedMeshData.ActiveBufferIndex ^ 1);
        }
#endif
    }
}
