using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Rendering
{
    internal class ComputeBufferWrapper<DataType> where DataType : struct
    {
        ComputeBuffer m_Buffer;
        ComputeShader m_Shader;
        public int PropertyID;

        public int BufferSize { get; private set; }

        public ComputeBufferWrapper(int namePropertyId, int size)
        {
            BufferSize = size;
            PropertyID = namePropertyId;
            m_Buffer = new ComputeBuffer(size, UnsafeUtility.SizeOf<DataType>(), ComputeBufferType.Default);
        }

        public ComputeBufferWrapper(int namePropertyId, int size, ComputeShader shader) : this(namePropertyId, size)
        {
            Assert.IsTrue(shader != null);
            m_Shader = shader;
        }

        public void Resize(int newSize)
        {
            BufferSize = newSize;
            m_Buffer.Dispose();
            m_Buffer = new ComputeBuffer(newSize, UnsafeUtility.SizeOf<DataType>(), ComputeBufferType.Default);
        }

        public void SetData(NativeArray<DataType> data, int nativeBufferStartIndex, int computeBufferStartIndex, int count)
        {
            m_Buffer.SetData(data, nativeBufferStartIndex, computeBufferStartIndex, count);
        }

        public void PushDataToGlobal()
        {
            Assert.IsTrue(m_Buffer.count > 0);
            Assert.IsTrue(m_Buffer.IsValid());
            Shader.SetGlobalBuffer(PropertyID, m_Buffer);
        }

        public void PushDataToKernel(int kernelIndex)
        {
            Assert.IsTrue(m_Buffer.count > 0 && m_Shader != null);
            Assert.IsTrue(m_Buffer.IsValid());
            m_Shader.SetBuffer(kernelIndex, PropertyID, m_Buffer);
        }

        public void Destroy()
        {
            BufferSize = -1;
            PropertyID = -1;
            m_Buffer.Dispose();
            m_Shader = null;
        }
    }

    internal class ComputeDoubleBufferWrapper<DataType> where DataType : struct
    {
        ComputeBufferWrapper<DataType>[] m_Buffers = new ComputeBufferWrapper<DataType>[2];
        readonly int m_PrimaryBufferPropertyID;
        readonly int m_SecondaryBufferPropertyID;

        public ComputeBufferWrapper<DataType> ActiveBuffer => m_Buffers[ActiveBufferIndex];
        public ComputeBufferWrapper<DataType> BackBuffer => m_Buffers[ActiveBufferIndex ^ 1];
        public int ActiveBufferIndex { get; private set; }

        public ComputeDoubleBufferWrapper(int primaryPropertyID, int secondaryPropertyID, int size)
        {
            m_PrimaryBufferPropertyID = primaryPropertyID;
            m_SecondaryBufferPropertyID = secondaryPropertyID;

            m_Buffers[0] = new ComputeBufferWrapper<DataType>(m_PrimaryBufferPropertyID, size);
            m_Buffers[1] = new ComputeBufferWrapper<DataType>(m_SecondaryBufferPropertyID, size);
        }

        public void PushDataToGlobal()
        {
            m_Buffers[0].PushDataToGlobal();
            m_Buffers[1].PushDataToGlobal();
        }

        public void SetActiveBuffer(int bufferIndex)
        {
            Assert.IsTrue(bufferIndex == 0 || bufferIndex == 1, "Invalid index for Mesh Deform buffers");

            // Assumes buffer index is changed correctly externally
            ActiveBufferIndex = bufferIndex;

            m_Buffers[bufferIndex].PropertyID = m_PrimaryBufferPropertyID;
            m_Buffers[bufferIndex ^ 1].PropertyID = m_SecondaryBufferPropertyID;

            m_Buffers[0].PushDataToGlobal();
            m_Buffers[1].PushDataToGlobal();
        }

        public void Destroy()
        {
            m_Buffers[0].Destroy();
            m_Buffers[1].Destroy();
        }
    }
}
