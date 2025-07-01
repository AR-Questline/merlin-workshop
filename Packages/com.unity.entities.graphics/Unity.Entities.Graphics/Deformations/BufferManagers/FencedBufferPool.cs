using System;
using Unity.Assertions;
using Unity.Collections;

using GraphicsBuffer = UnityEngine.GraphicsBuffer;

namespace Unity.Rendering
{
    internal class FencedBufferPool : IDisposable
    {
        struct FrameData
        {
            public int DataBufferID;
        }

        public int BufferSize { get; private set; }

        NativeQueue<FrameData> m_FrameData;

        BufferPool m_DataBufferPool;

        int m_CurrentFrameBufferID;

        public FencedBufferPool()
        {
            m_FrameData = new NativeQueue<FrameData>(Allocator.Persistent);
            m_CurrentFrameBufferID = -1;
        }

        public void Dispose()
        {
            if (m_FrameData.IsCreated)
                m_FrameData.Dispose();

            m_DataBufferPool?.Dispose();

            m_CurrentFrameBufferID = -1;
        }

        public void BeginFrame()
        {
            Assert.IsTrue(m_CurrentFrameBufferID == -1);

            RecoverBuffers();
            m_CurrentFrameBufferID = m_DataBufferPool.GetBufferId();
        }

        public void EndFrame()
        {
            Assert.IsFalse(m_CurrentFrameBufferID == -1);

            var frameData = new FrameData
            {
                DataBufferID = m_CurrentFrameBufferID,
            };

            m_FrameData.Enqueue(frameData);

            m_CurrentFrameBufferID = -1;
        }

        public GraphicsBuffer GetCurrentFrameBuffer()
        {
            Assert.IsFalse(m_CurrentFrameBufferID == -1);
            return m_DataBufferPool.GetBufferFromId(m_CurrentFrameBufferID);
        }

        public void ResizeBuffer(int size, int stride)
        {
            m_FrameData.Clear();

            m_DataBufferPool?.Dispose();
            m_DataBufferPool = new BufferPool(size, stride, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.LockBufferForWrite);

            BufferSize = size;
        }

        void RecoverBuffers()
        {
            while (CanFreeNextBuffer())
            {
                var data = m_FrameData.Dequeue();

                m_DataBufferPool.PutBufferId(data.DataBufferID);
            }

            bool CanFreeNextBuffer()
            {
                return m_FrameData.Count > SparseUploader.NumFramesInFlight + 1;
            }
        }
    }
}
