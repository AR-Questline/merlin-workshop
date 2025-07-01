using Unity.Assertions;
using Unity.Deformations;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    [RequireMatchingQueriesForUpdate]
    partial class BlendShapeDeformationSystem : SystemBase
    {
        static readonly ProfilerMarker k_FinalizePushBlendShapeWeight = new ProfilerMarker("FinalizeBlendWeightForGPU");
        static readonly ProfilerMarker k_BlendShapeDeformationMarker = new ProfilerMarker("BlendShapeDeformationDispatch");

        static readonly int k_VertexCount = Shader.PropertyToID("g_VertexCount");
        static readonly int k_DeformedMeshStartIndex = Shader.PropertyToID("g_DeformedMeshStartIndex");
        static readonly int k_InstanceCount = Shader.PropertyToID("g_InstanceCount");
        static readonly int k_BlendShapeCount = Shader.PropertyToID("g_BlendShapeCount");
        static readonly int k_BlendShapeWeightStartIndex = Shader.PropertyToID("g_BlendShapeWeightStartIndex");
        static readonly int k_BlendShapeVerticesBuffer = Shader.PropertyToID("_BlendShapeVertexData");

        ComputeShader m_ComputeShader;
        PushMeshDataSystem m_PushMeshDataSystem;
        EntitiesGraphicsSystem m_RendererSystem;

        int m_Kernel;

        EntityQuery m_BlendWeightQuery;

        protected override void OnCreate()
        {
#if !HYBRID_RENDERER_DISABLED
            if (!EntitiesGraphicsUtils.IsEntitiesGraphicsSupportedOnSystem())
#endif
            {
                Enabled = false;
                UnityEngine.Debug.Log("No SRP present, no compute shader support, or running with -nographics. Mesh Deformation Systems disabled.");
                return;
            }

            m_PushMeshDataSystem = World.GetOrCreateSystemManaged<PushMeshDataSystem>();
            Assert.IsNotNull(m_PushMeshDataSystem, $"{nameof(PushMeshDataSystem)} was not found!");

            m_ComputeShader = Resources.Load<ComputeShader>("BlendShapeComputeShader");
            Assert.IsNotNull(m_ComputeShader, $"Compute shader for {typeof(BlendShapeDeformationSystem)} was not found!");

            m_RendererSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            Assert.IsNotNull(m_RendererSystem, $"{nameof(EntitiesGraphicsSystem)} was not found!");

            m_Kernel = m_ComputeShader.FindKernel("BlendShapeComputeKernel");

            m_BlendWeightQuery = GetEntityQuery(
                ComponentType.ReadWrite<BlendShapeWeight>()
            );
        }

        protected override void OnUpdate()
        {
            if (m_PushMeshDataSystem.BlendShapeWeightCount == 0)
                return;

            k_FinalizePushBlendShapeWeight.Begin();

            // Complete the Read/Write dependency on BlendShapeWeight.
            // This guarantees that the data has been written to GPU
            // Assuming that PushBlendWeightSystem has executed before this system.
            m_BlendWeightQuery.CompleteDependency();
            m_PushMeshDataSystem.BlendShapeBufferManager.UnlockBlendWeightBufferForWrite(m_PushMeshDataSystem.BlendShapeWeightCount);

            k_FinalizePushBlendShapeWeight.End();
            k_BlendShapeDeformationMarker.Begin();

            foreach (var deformationBatch in m_PushMeshDataSystem.DeformationBatches)
            {
                var id = deformationBatch.Key;
                var batchData = deformationBatch.Value;

                var hasMeshData = m_PushMeshDataSystem.TryGetSharedMeshData(id, out var meshData);

                Assert.IsTrue(hasMeshData);

                if (!meshData.HasBlendShapes)
                    continue;

                m_ComputeShader.SetInt(k_VertexCount, meshData.VertexCount);
                m_ComputeShader.SetInt(k_BlendShapeCount, meshData.BlendShapeCount);
                m_ComputeShader.SetInt(k_DeformedMeshStartIndex, batchData.MeshVertexIndex);
                m_ComputeShader.SetInt(k_BlendShapeWeightStartIndex, batchData.BlendShapeIndex);
                m_ComputeShader.SetInt(k_InstanceCount, batchData.InstanceCount);

                var mesh = m_RendererSystem.GetMesh(meshData.MeshID);
                var blendShapeBuffer = mesh.GetBlendShapeBuffer(BlendShapeBufferLayout.PerVertex);
                Assert.IsNotNull(blendShapeBuffer);

                m_ComputeShader.SetBuffer(m_Kernel, k_BlendShapeVerticesBuffer, blendShapeBuffer);
                m_ComputeShader.Dispatch(m_Kernel, 1024, 1, 1);

                blendShapeBuffer.Dispose();
            }

            k_BlendShapeDeformationMarker.End();
        }
    }
}
