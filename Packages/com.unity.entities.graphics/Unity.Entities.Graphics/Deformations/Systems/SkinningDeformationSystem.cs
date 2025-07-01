using Unity.Assertions;
using Unity.Deformations;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace Unity.Rendering
{
    [RequireMatchingQueriesForUpdate]
    partial class SkinningDeformationSystem : SystemBase
    {
        static readonly ProfilerMarker k_FinalizePushSkinMatrix = new ProfilerMarker("FinalizeSkinMatrixForGPU");
        static readonly ProfilerMarker k_SkinningDeformationMarker = new ProfilerMarker("SkinningDeformationDispatch");

        static readonly int k_VertexCount = Shader.PropertyToID("g_VertexCount");
        static readonly int k_DeformedMeshStartIndex = Shader.PropertyToID("g_DeformedMeshStartIndex");
        static readonly int k_InstancesCount = Shader.PropertyToID("g_InstanceCount");
        static readonly int k_SharedMeshBoneCount = Shader.PropertyToID("g_SharedMeshBoneCount");
        static readonly int k_SkinMatricesStartIndex = Shader.PropertyToID("g_SkinMatricesStartIndex");
        static readonly int k_SharedMeshBoneWeightsBuffer = Shader.PropertyToID("_SharedMeshBoneWeights");

        ComputeShader m_ComputeShader;
        PushMeshDataSystem m_PushMeshDataSystem;
        EntitiesGraphicsSystem m_RendererSystem;

        int m_KernelDense1;
        int m_KernelDense2;
        int m_KernelDense4;
        int m_KernelSparse;

        EntityQuery m_SkinMatrixQuery;

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

            m_ComputeShader = Resources.Load<ComputeShader>("SkinningComputeShader");
            Assert.IsNotNull(m_ComputeShader, $"Compute shader for {typeof(SkinningDeformationSystem)} was not found!");

            m_RendererSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            Assert.IsNotNull(m_RendererSystem, $"{nameof(EntitiesGraphicsSystem)} was not found!");

            m_KernelDense1 = m_ComputeShader.FindKernel("SkinningDense1ComputeKernel");
            m_KernelDense2 = m_ComputeShader.FindKernel("SkinningDense2ComputeKernel");
            m_KernelDense4 = m_ComputeShader.FindKernel("SkinningDense4ComputeKernel");
            m_KernelSparse = m_ComputeShader.FindKernel("SkinningSparseComputeKernel");

            m_SkinMatrixQuery = GetEntityQuery(
                ComponentType.ReadWrite<SkinMatrix>()
            );
        }

        protected override void OnUpdate()
        {
            if (m_PushMeshDataSystem.SkinMatrixCount == 0)
                return;

            k_FinalizePushSkinMatrix.Begin();

            // Complete the Read/Write dependency on SkinMatrix.
            // This guarantees that the data has been written to GPU
            // Assuming that PushSkinMatrixSystem has executed before this system.
            m_SkinMatrixQuery.CompleteDependency();
            m_PushMeshDataSystem.SkinningBufferManager.UnlockSkinMatrixBufferForWrite(m_PushMeshDataSystem.SkinMatrixCount);

            k_FinalizePushSkinMatrix.End();
            k_SkinningDeformationMarker.Begin();

            foreach (var deformationBatch in m_PushMeshDataSystem.DeformationBatches)
            {
                var id = deformationBatch.Key;
                var batchData = deformationBatch.Value;

                var hasMeshData = m_PushMeshDataSystem.TryGetSharedMeshData(id, out var meshData);

                Assert.IsTrue(hasMeshData);

                if (!meshData.HasSkinning)
                    continue;

                m_ComputeShader.SetInt(k_VertexCount, meshData.VertexCount);
                m_ComputeShader.SetInt(k_SharedMeshBoneCount, meshData.BoneCount);
                m_ComputeShader.SetInt(k_DeformedMeshStartIndex, batchData.MeshVertexIndex);
                m_ComputeShader.SetInt(k_SkinMatricesStartIndex, batchData.SkinMatrixIndex);
                m_ComputeShader.SetInt(k_InstancesCount, batchData.InstanceCount);

                var mesh = m_RendererSystem.GetMesh(meshData.MeshID);
                var skinWeightLayout = mesh.skinWeightBufferLayout;
                Assert.IsFalse(skinWeightLayout == SkinWeights.None);
                var skinWeightBuffer = mesh.GetBoneWeightBuffer(skinWeightLayout);
                Assert.IsNotNull(skinWeightBuffer);

                var kernel = skinWeightLayout switch
                {
                    SkinWeights.OneBone => m_KernelDense1,
                    SkinWeights.TwoBones => m_KernelDense2,
                    SkinWeights.FourBones => m_KernelDense4,
                    SkinWeights.Unlimited => m_KernelSparse,
                    _ => m_KernelDense1,
                };

                m_ComputeShader.SetBuffer(kernel, k_SharedMeshBoneWeightsBuffer, skinWeightBuffer);
                m_ComputeShader.Dispatch(kernel, 1024, 1, 1);

                skinWeightBuffer.Dispose();
            }

            k_SkinningDeformationMarker.End();
        }
    }
}
