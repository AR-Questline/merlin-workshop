using Unity.Assertions;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace Unity.Rendering
{
    [RequireMatchingQueriesForUpdate]
    partial class InstantiateDeformationSystem : SystemBase
    {
        static readonly ProfilerMarker k_InstantiateDeformationMarker = new ProfilerMarker("InstantiateDeformationSystem");

        static readonly int k_VertexCount = Shader.PropertyToID("g_VertexCount");
        static readonly int k_DeformedMeshStartIndex = Shader.PropertyToID("g_DeformedMeshStartIndex");
        static readonly int k_InstancesCount = Shader.PropertyToID("g_InstanceCount");
        static readonly int k_SharedMeshVertexBuffer = Shader.PropertyToID("_SharedMeshVertexBuffer");

        ComputeShader m_ComputeShader;
        PushMeshDataSystem m_PushMeshDataSystem;
        EntitiesGraphicsSystem m_RendererSystem;

        int m_kernel;

        EntityQuery m_Query;

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

            m_ComputeShader = Resources.Load<ComputeShader>("InstantiateDeformationData");
            Assert.IsNotNull(m_ComputeShader, $"Compute shader for {typeof(InstantiateDeformationSystem)} was not found!");

            m_PushMeshDataSystem = World.GetOrCreateSystemManaged<PushMeshDataSystem>();
            Assert.IsNotNull(m_PushMeshDataSystem, $"{nameof(PushMeshDataSystem)} was not found!");

            m_RendererSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            Assert.IsNotNull(m_RendererSystem, $"{nameof(EntitiesGraphicsSystem)} was not found!");

            m_kernel = m_ComputeShader.FindKernel("InstantiateDeformationDataKernel");

            m_Query = GetEntityQuery(
                ComponentType.ReadOnly<SharedMeshTracker>(),
                ComponentType.ReadOnly<DeformedMeshIndex>()
            );
        }

        protected override void OnUpdate()
        {
            k_InstantiateDeformationMarker.Begin();

            foreach (var deformationBatch in m_PushMeshDataSystem.DeformationBatches)
            {
                var id = deformationBatch.Key;
                var batchData = deformationBatch.Value;

                var hasMeshData = m_PushMeshDataSystem.TryGetSharedMeshData(id, out var meshData);

                Assert.IsTrue(hasMeshData);

                m_ComputeShader.SetInt(k_VertexCount, meshData.VertexCount);
                m_ComputeShader.SetInt(k_DeformedMeshStartIndex, batchData.MeshVertexIndex);
                m_ComputeShader.SetInt(k_InstancesCount, batchData.InstanceCount);

                var mesh = m_RendererSystem.GetMesh(meshData.MeshID);
                var vertexBuffer = mesh.GetVertexBuffer(0);
                Assert.IsNotNull(vertexBuffer);

                m_ComputeShader.SetBuffer(m_kernel, k_SharedMeshVertexBuffer, vertexBuffer);
                m_ComputeShader.Dispatch(m_kernel, 1024, 1, 1);
                vertexBuffer.Dispose();
            }

            k_InstantiateDeformationMarker.End();
        }
    }
}
