using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering
{
    internal class BlendShapeBufferManager
    {
        const int k_ChunkSize = 2048;

        static readonly int k_BlendShapeWeightsBuffer = Shader.PropertyToID("_BlendShapeWeights");
        static readonly int k_MaxSize = (int)math.min(SystemInfo.maxGraphicsBufferSize / UnsafeUtility.SizeOf<float>(), int.MaxValue);

        FencedBufferPool m_BufferPool;

        public BlendShapeBufferManager()
        {
            m_BufferPool = new FencedBufferPool();
        }

        public void Dispose()
        {
            m_BufferPool.Dispose();
        }

        public bool ResizePassBufferIfRequired(int requiredSize)
        {
            var size = m_BufferPool.BufferSize;
            if (size <= requiredSize || size - requiredSize > k_ChunkSize)
            {
                var newSize = ((requiredSize / k_ChunkSize) + 1) * k_ChunkSize;

                if (newSize > k_MaxSize)
                {
                    // Only inform users if the content requires a buffer that is too big.
                    if (requiredSize > k_MaxSize)
                        UnityEngine.Debug.LogWarning("The world contains too many blend shapes to fit into a single GraphicsBuffer. Not all deformed meshes are guaranteed to render correctly. Reduce the number of active deformed meshes.");

                    // Do not actually resize the buffer if we are already at max capacity.
                    if (size == k_MaxSize)
                        return false;

                    newSize = k_MaxSize;
                }

                m_BufferPool.ResizeBuffer(newSize, UnsafeUtility.SizeOf<float>());
                return true;
            }

            return false;
        }

        public NativeArray<float> LockBlendWeightBufferForWrite(int count)
        {
            m_BufferPool.BeginFrame();
            var buffer = m_BufferPool.GetCurrentFrameBuffer();
            return buffer.LockBufferForWrite<float>(0, count);
        }

        public void UnlockBlendWeightBufferForWrite(int count)
        {
            var buffer = m_BufferPool.GetCurrentFrameBuffer();
            buffer.UnlockBufferAfterWrite<float>(count);
            Shader.SetGlobalBuffer(k_BlendShapeWeightsBuffer, buffer);
            m_BufferPool.EndFrame();
        }       
    }
}
