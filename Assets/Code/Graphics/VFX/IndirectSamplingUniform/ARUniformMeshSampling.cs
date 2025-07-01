using Awaken.TG.Main.General.Configs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX.IndirectSamplingUniform {
    public class ARUniformMeshSampling {
        const string CalculateTrianglesAreasUIntKernelName = "CalculateTrianglesAreasUInt";
        const string CalculateTrianglesAreasUShortKernelName = "CalculateTrianglesAreasUShort";
        const string AccumulateTrianglesAreasKernelName = "AccumulateTrianglesAreas";
        const string SampleMeshKernelName = "SampleMesh";

        static readonly int VertexPositionsStreamBufferID = Shader.PropertyToID("VertexPositionsStreamBuffer");
        static readonly int VertexPositionStrideID = Shader.PropertyToID("VertexPositionStride");
        static readonly int VertexPositionOffsetID = Shader.PropertyToID("VertexPositionOffset");

        static readonly int VertexUVStreamBufferID = Shader.PropertyToID("VertexUVStreamBuffer");
        static readonly int VertexUVStrideID = Shader.PropertyToID("VertexUVStride");
        static readonly int VertexUVOffsetID = Shader.PropertyToID("VertexUVOffset");

        static readonly int IndicesBufferUIntID = Shader.PropertyToID("IndicesBufferUInt");
        static readonly int IndicesBufferUShortID = Shader.PropertyToID("IndicesBufferUShort");
        static readonly int TrianglesCountID = Shader.PropertyToID("TrianglesCount");

        static readonly int TrianglesID = Shader.PropertyToID("Triangles");
        static readonly int AccumulatedTriangleAreaID = Shader.PropertyToID("AccumulatedTriangleArea");

        static readonly int SeedID = Shader.PropertyToID("Seed");
        static readonly int SamplesCountID = Shader.PropertyToID("SamplesCount");
        static readonly int SamplesID = Shader.PropertyToID("Samples");

        AsyncGPUReadbackRequest _vertexPositionsRequest;
        AsyncGPUReadbackRequest _vertexUVsDataRequest;
        AsyncGPUReadbackRequest _indicesDataRequest;

        NativeArray<byte> _vertexPositionsData;
        NativeArray<byte> _vertexUVsData;
        NativeArray<byte> _indicesData;

        int _trianglesCount;

        Mesh _mesh;

        public unsafe GraphicsBuffer StartSampling(Mesh mesh, int samplesCount) {
            _mesh = mesh;

            var samplesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, samplesCount, sizeof(SampledPositionAndUV));

            var vertexPositionStreamIndex = _mesh.GetVertexAttributeStream(VertexAttribute.Position);
            var vertexUVStreamIndex = _mesh.GetVertexAttributeStream(VertexAttribute.TexCoord0);

            var meshVertexPositionsStreamBuffer = _mesh.GetVertexBuffer(vertexPositionStreamIndex);

            var positionDataSize = meshVertexPositionsStreamBuffer.count * meshVertexPositionsStreamBuffer.stride;
            _vertexPositionsData = new NativeArray<byte>(positionDataSize, Allocator.Persistent);
            _vertexPositionsRequest = AsyncGPUReadback.RequestIntoNativeArray(ref _vertexPositionsData, meshVertexPositionsStreamBuffer);
            meshVertexPositionsStreamBuffer.Dispose();

            if (vertexPositionStreamIndex == vertexUVStreamIndex) {
                _vertexUVsData = _vertexPositionsData;
                _vertexUVsDataRequest = _vertexPositionsRequest;
            } else {
                var meshVertexUVStreamBuffer = _mesh.GetVertexBuffer(vertexUVStreamIndex);
                var uvsDataSize = meshVertexUVStreamBuffer.count * meshVertexUVStreamBuffer.stride;
                _vertexUVsData = new NativeArray<byte>(uvsDataSize, Allocator.Persistent);
                _vertexUVsDataRequest = AsyncGPUReadback.RequestIntoNativeArray(ref _vertexUVsData, meshVertexUVStreamBuffer);
                meshVertexUVStreamBuffer.Dispose();
            }

            var meshIndicesBuffer = _mesh.GetIndexBuffer();
            var indicesDataSize = EnsureValidStride(meshIndicesBuffer.count * meshIndicesBuffer.stride);
            _trianglesCount = meshIndicesBuffer.count / 3;
            _indicesData = new NativeArray<byte>(indicesDataSize, Allocator.Persistent);
            _indicesDataRequest = AsyncGPUReadback.RequestIntoNativeArray(ref _indicesData, meshIndicesBuffer);
            meshIndicesBuffer.Dispose();

            return samplesBuffer;
        }

        public unsafe void UpdateLoading(GraphicsBuffer samplesBuffer, ref ARUniformMeshSampling self) {
            if (!_vertexPositionsRequest.done || !_vertexUVsDataRequest.done || !_indicesDataRequest.done) {
                return;
            }

            var vertexPositionStreamIndex = _mesh.GetVertexAttributeStream(VertexAttribute.Position);
            var vertexUVStreamIndex = _mesh.GetVertexAttributeStream(VertexAttribute.TexCoord0);

            var samplesCount = samplesBuffer.count;

            var preparer = GameConstants.Get.uniformMeshPreparerComputeShader;
            var samplerShader = GameConstants.Get.uniformMeshSamplerComputeShader;

            CreateMeshGpuData(_vertexPositionsData, _vertexUVsData, _indicesData,
                out var vertexPositionsBuffer, out var vertexUVsBuffer, out var indicesBuffer);

            _vertexPositionsData.Dispose();
            if (vertexPositionStreamIndex != vertexUVStreamIndex) {
                _vertexUVsData.Dispose();
            }
            _indicesData.Dispose();

            var isUIntIndices = _mesh.indexFormat == IndexFormat.UInt32;

            // -- Dispatch trianglesAndAreaKernel
            var trianglesAndAreaKernel = isUIntIndices ?
                preparer.FindKernel(CalculateTrianglesAreasUIntKernelName) :
                preparer.FindKernel(CalculateTrianglesAreasUShortKernelName);

            preparer.SetBuffer(trianglesAndAreaKernel, VertexPositionsStreamBufferID, vertexPositionsBuffer);
            preparer.SetInt(VertexPositionStrideID, _mesh.GetVertexBufferStride(vertexPositionStreamIndex));
            preparer.SetInt(VertexPositionOffsetID, _mesh.GetVertexAttributeOffset(VertexAttribute.Position));

            preparer.SetBuffer(trianglesAndAreaKernel, VertexUVStreamBufferID, vertexUVsBuffer);
            preparer.SetInt(VertexUVStrideID, _mesh.GetVertexBufferStride(vertexUVStreamIndex));
            preparer.SetInt(VertexUVOffsetID, _mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0));

            if (isUIntIndices) {
                preparer.SetBuffer(trianglesAndAreaKernel, IndicesBufferUIntID, indicesBuffer);
            } else {
                preparer.SetBuffer(trianglesAndAreaKernel, IndicesBufferUShortID, indicesBuffer);
            }
            preparer.SetInt(TrianglesCountID, _trianglesCount);

            var trianglesWithUVsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _trianglesCount, sizeof(TriangleWithUV));
            preparer.SetBuffer(trianglesAndAreaKernel, TrianglesID, trianglesWithUVsBuffer);
            var accumulatedTriangleArea = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _trianglesCount, sizeof(float));
            preparer.SetBuffer(trianglesAndAreaKernel, AccumulatedTriangleAreaID, accumulatedTriangleArea);

            preparer.Dispatch(trianglesAndAreaKernel, Mathf.CeilToInt(_trianglesCount / 128f), 1, 1);

            // -- Dispose mesh data buffers
            vertexPositionsBuffer.Release();
            if (vertexPositionStreamIndex != vertexUVStreamIndex) {
                vertexUVsBuffer.Release();
            }
            indicesBuffer.Release();

            // -- Dispatch accumulation
            var accumulateKernel = preparer.FindKernel(AccumulateTrianglesAreasKernelName);
            preparer.SetBuffer(accumulateKernel, AccumulatedTriangleAreaID, accumulatedTriangleArea);
            preparer.Dispatch(accumulateKernel, 1, 1, 1);

            // -- Dispatch sampling
            var sampleMeshKernel = samplerShader.FindKernel(SampleMeshKernelName);
            samplerShader.SetBuffer(sampleMeshKernel, TrianglesID, trianglesWithUVsBuffer);
            samplerShader.SetInt(TrianglesCountID, _trianglesCount);
            samplerShader.SetBuffer(sampleMeshKernel, AccumulatedTriangleAreaID, accumulatedTriangleArea);
            samplerShader.SetInt(SeedID, 69);
            samplerShader.SetInt(SamplesCountID, samplesCount);

            samplerShader.SetBuffer(sampleMeshKernel, SamplesID, samplesBuffer);

            samplerShader.Dispatch(sampleMeshKernel, Mathf.CeilToInt(samplesCount / 128f), 1, 1);

            // -- Dispose intermediate buffers
            accumulatedTriangleArea.Release();
            trianglesWithUVsBuffer.Release();

            _vertexPositionsRequest = default;
            _vertexUVsDataRequest = default;
            _indicesDataRequest = default;

            self = null;
            _mesh = null;
        }

        public void Dispose() {
            // Do nothing if already disposed
            if (_mesh == null) {
                return;
            }

            AsyncGPUReadback.WaitAllRequests();
            var vertexPositionStreamIndex = _mesh.GetVertexAttributeStream(VertexAttribute.Position);
            var vertexUVStreamIndex = _mesh.GetVertexAttributeStream(VertexAttribute.TexCoord0);

            _vertexPositionsData.Dispose();
            if (vertexPositionStreamIndex != vertexUVStreamIndex) {
                _vertexUVsData.Dispose();
            }
            _indicesData.Dispose();

            _mesh = null;
        }

        static unsafe void CreateMeshGpuData(
            NativeArray<byte> vertexPositionsData, NativeArray<byte> vertexUVsData, NativeArray<byte> indicesData,
            out GraphicsBuffer vertexPositionsBuffer, out GraphicsBuffer vertexUVsBuffer, out GraphicsBuffer indicesBuffer) {
            vertexPositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, vertexPositionsData.Length / 4, 4);
            vertexPositionsBuffer.SetData(vertexPositionsData);

            var positionAndUVsInSingleBuffer = vertexPositionsData.GetUnsafePtr() == vertexUVsData.GetUnsafePtr();
            if (positionAndUVsInSingleBuffer) {
                vertexUVsBuffer = vertexPositionsBuffer;
            } else {
                vertexUVsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, vertexUVsData.Length / 4, 4);
                vertexUVsBuffer.SetData(vertexUVsData);

            }

            indicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, indicesData.Length / 4, 4);
            indicesBuffer.SetData(indicesData);
        }

        static int EnsureValidStride(int bufferSize) {
            return (bufferSize + 3) & ~3;
        }

        struct TriangleWithUV {
            public float3 v0Pos;
            public float2 v0UV;

            public float3 v1Pos;
            public float2 v1UV;

            public float3 v2Pos;
            public float2 v2UV;
        }
    }
}