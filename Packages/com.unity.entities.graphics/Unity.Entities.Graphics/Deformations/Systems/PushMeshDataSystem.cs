using System.Threading;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Burst.Intrinsics;

using BatchMeshID = UnityEngine.Rendering.BatchMeshID;
using VertexAttribute = UnityEngine.Rendering.VertexAttribute;
using VertexAttributeFormat = UnityEngine.Rendering.VertexAttributeFormat;

namespace Unity.Rendering
{
    struct MeshDeformationBatch
    {
        public int BatchIndex;
        public int MeshVertexIndex;
        public int BlendShapeIndex;
        public int SkinMatrixIndex;
        public int InstanceCount;
    }

    [RequireMatchingQueriesForUpdate]
    partial class PushMeshDataSystem : SystemBase
    {
        static readonly ProfilerMarker k_ChangesMarker = new ProfilerMarker("Detect Entity Changes");
        static readonly ProfilerMarker k_OutputCountBuffer = new ProfilerMarker("Counting Deformed Mesh Buffer");
        static readonly ProfilerMarker k_OutputResizeBuffer = new ProfilerMarker("Resize Deformed Mesh Buffer");
        static readonly ProfilerMarker k_CollectActiveMeshes = new ProfilerMarker("Collect Active Deformations");

        // Reserve index 0 for 'uninitialized'
        const int k_DeformBufferStartIndex = 1;

        internal int SkinMatrixCount => m_SkinMatrixCount.Value;
        internal int BlendShapeWeightCount => m_BlendShapeWeightCount.Value;

        // Active deformation batches for this frame.
        internal NativeParallelHashMap<BatchMeshID, MeshDeformationBatch> DeformationBatches;

        // The MeshID and data of shared meshes are copied into two parallel arrays.
        // For each ID-data pair, the MeshID is stored in `m_MeshIDs[i]`
        // while the data is stored in `m_SharedMeshData[i]` (for the same `i`).
        NativeList<BatchMeshID> m_MeshIDs;
        NativeList<SharedMeshData> m_SharedMeshData;

        NativeReference<int> m_MeshVertexCount;
        NativeReference<int> m_BlendShapeWeightCount;
        NativeReference<int> m_SkinMatrixCount;

        internal SkinningBufferManager SkinningBufferManager { get; private set; }

        internal BlendShapeBufferManager BlendShapeBufferManager { get; private set; }

        MeshBufferManager m_MeshBufferManager;
        EntitiesGraphicsSystem m_RendererSystem;

        JobHandle m_BatchConstructionHandle;

        EntityQuery m_LayoutDeformedMeshesQuery;

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

            // Setup data structures
            DeformationBatches = new NativeParallelHashMap<BatchMeshID, MeshDeformationBatch>(64, Allocator.Persistent);
            m_MeshIDs = new NativeList<BatchMeshID>(64, Allocator.Persistent);
            m_SharedMeshData = new NativeList<SharedMeshData>(64, Allocator.Persistent);

            m_MeshVertexCount = new NativeReference<int>(Allocator.Persistent);
            m_BlendShapeWeightCount = new NativeReference<int>(Allocator.Persistent);
            m_SkinMatrixCount = new NativeReference<int>(Allocator.Persistent);

            // Create GPU Buffers
            m_MeshBufferManager = new MeshBufferManager();
            BlendShapeBufferManager = new BlendShapeBufferManager();
            SkinningBufferManager = new SkinningBufferManager();

            // Gather references
            m_RendererSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

            // Queries
            m_LayoutDeformedMeshesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<DeformedEntity>(),
                    ComponentType.ReadOnly<MaterialMeshInfo>(),
                    ComponentType.ReadWrite<DeformedMeshIndex>(),
                },
                Any = new[]
                {
                    ComponentType.ReadWrite<BlendWeightBufferIndex>(),
                    ComponentType.ReadWrite<SkinMatrixBufferIndex>(),
                },
                Options = EntityQueryOptions.IgnoreComponentEnabledState
            });
        }

        protected override void OnDestroy()
        {
            if (m_SharedMeshData.IsCreated)
                m_SharedMeshData.Dispose();
            if (m_MeshIDs.IsCreated)
                m_MeshIDs.Dispose();
            if (DeformationBatches.IsCreated)
                DeformationBatches.Dispose();
            if (m_MeshVertexCount.IsCreated)
                m_MeshVertexCount.Dispose();
            if (m_BlendShapeWeightCount.IsCreated)
                m_BlendShapeWeightCount.Dispose();
            if (m_SkinMatrixCount.IsCreated)
                m_SkinMatrixCount.Dispose();

            m_MeshBufferManager?.Dispose();
            BlendShapeBufferManager?.Dispose();
            SkinningBufferManager?.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
            m_MeshBufferManager.FlipDeformedMeshBuffer();
#endif

            // Handle newly spawned and removed entities.
            AddRemoveTrackedMeshes();

            // Find active deformed meshes for this frame,
            // construct deformation batches and allocate
            // them in the graphics buffers.
            LayoutOutputBuffer();

            // Complete the constructions of batches
            // So that we can resize the output buffers
            // and know the final buffer sizes required
            m_BatchConstructionHandle.Complete();
            FitOutputBuffer();
        }

        // Support more layouts: GFXMESH-66.
        private bool HasSupportedVertexLayout(UnityEngine.Mesh mesh)
        {
            // There need to be at least 3 vertex attributes
            if (mesh.vertexAttributeCount < 3)
            {
                UnityEngine.Debug.LogWarning($"Unsupported vertex layout for deformations in mesh ({mesh.name}). Expecting Position, Normal and Tangent attributes but only found {mesh.vertexAttributeCount} attributes. The mesh deformations for this mesh will be disabled and rendered as a regular mesh.");
                return false;
            }

            var position = mesh.GetVertexAttribute(0);
            var normal = mesh.GetVertexAttribute(1);
            var tangent = mesh.GetVertexAttribute(2);

            // The attributes are Position, Normal & Tangent.
            if (position.attribute != VertexAttribute.Position || normal.attribute != VertexAttribute.Normal || tangent.attribute != VertexAttribute.Tangent)
            {
                UnityEngine.Debug.LogWarning($"Unsupported vertex layout for deformations in mesh ({mesh.name}). Expecting Position, Normal and Tangent attributes but found {position.attribute}, {normal.attribute} and {tangent.attribute}. The mesh deformations for this mesh will be disabled and rendered as a regular mesh.");
                return false;
            }

            // The format for these attributes is float32.
            if (position.format != VertexAttributeFormat.Float32 || normal.format != VertexAttributeFormat.Float32 || tangent.format != VertexAttributeFormat.Float32)
            {
                UnityEngine.Debug.LogWarning($"Unsupported vertex layout for deformations in mesh ({mesh.name}). The Position, Normal and Tangent attributes need to use Float32 format. The mesh deformations for this mesh will be disabled and rendered as a regular mesh.");
                return false;
            }

            // The dimension needs to be 3 for position and normal and 4 for tangent.
            if (position.dimension != 3 || normal.dimension != 3 || tangent.dimension != 4)
            {
                UnityEngine.Debug.LogWarning($"Unsupported vertex layout for deformations in mesh ({mesh.name}). Expecting dimensions 3, 3 and 4 for Position, Normal and Tangent respectively but got {position.dimension}, {normal.dimension} and {tangent.dimension}. The mesh deformations for this mesh will be disabled and rendered as a regular mesh.");
                return false;
            }

            // All three attributes need to be in stream 0
            if (position.stream != 0 || normal.stream != 0 || tangent.stream != 0)
            {
                UnityEngine.Debug.LogWarning($"Unsupported vertex layout for deformations in mesh ({mesh.name}). The Position, Normal and Tangent attributes need to be present in stream 0. The mesh deformations for this mesh will be disabled and rendered as a regular mesh.");
                return false;
            }

            // There cannot be any other attributes in stream 0
            for (int i = 3; i < mesh.vertexAttributeCount; i++)
            {
                var attribute = mesh.GetVertexAttribute(i);
                if (attribute.stream == 0)
                {
                    UnityEngine.Debug.LogWarning($"Unsupported vertex layout for deformations in mesh ({mesh.name}). The vertex attribute {attribute.attribute} should not be present in stream 0. The mesh deformations for this mesh will be disabled and rendered as a regular mesh.");
                    return false;
                }
            }

            return true;
        }

        private SharedMeshData AddSharedMeshData(in BatchMeshID meshID, UnityEngine.Mesh mesh, NativeList<BatchMeshID> meshIDs, NativeList<SharedMeshData> meshData)
        {
            Assert.IsFalse(meshID == BatchMeshID.Null);
            Assert.IsNotNull(mesh);

            mesh.vertexBufferTarget |= UnityEngine.GraphicsBuffer.Target.Raw;

            var data = new SharedMeshData
            {
                VertexCount = mesh.vertexCount,
                BlendShapeCount = mesh.blendShapeCount,
                BoneCount = mesh.bindposeCount,
                MeshID = meshID,
                RefCount = 1,
            };

            // Insert the MeshData sorted by the Hash
            var hash = data.StateHash();
            var index = 0;

            while (index < meshData.Length && hash > meshData[index].StateHash())
                index++;

            if (meshIDs.Length > 0 && index != meshIDs.Length)
            {
                meshIDs.InsertRangeWithBeginEnd(index, index + 1);
                meshData.InsertRangeWithBeginEnd(index, index + 1);

                meshIDs[index] = data.MeshID;
                meshData[index] = data;
            }
            else
            {
                meshIDs.Add(data.MeshID);
                meshData.Add(data);
            }

            return data;
        }

        internal bool TryGetSharedMeshData(BatchMeshID id, out SharedMeshData data)
        {
            Assert.IsTrue(m_MeshIDs.IsCreated && m_SharedMeshData.IsCreated);
            Assert.IsTrue(m_MeshIDs.Length == m_SharedMeshData.Length);
            Assert.IsFalse(id == BatchMeshID.Null);

            var index = m_MeshIDs.IndexOf(id);

            if (index < 0)
            {
                data = default;
                return false;
            }

            data = m_SharedMeshData[index];
            return true;
        }

        private void AddRemoveTrackedMeshes()
        {
            k_ChangesMarker.Begin();

            var rmvMeshes = new NativeList<int>(Allocator.TempJob);
            var addMeshes = new NativeList<int>(Allocator.TempJob);

            var meshData = m_SharedMeshData;
            var meshIDs = m_MeshIDs;

            Entities
                .WithName("DisabledComponents")
                .WithAll<Disabled, DeformedEntity>()
                .WithStructuralChanges()
                .ForEach((Entity e, in SharedMeshTracker tracked) =>
                {
                    EntityManager.RemoveComponent<SharedMeshTracker>(e);
                    EntityManager.RemoveComponent<DeformedMeshIndex>(e);

                    // These components may or may not exist.
                    EntityManager.RemoveComponent<BlendWeightBufferIndex>(e);
                    EntityManager.RemoveComponent<SkinMatrixBufferIndex>(e);

                    rmvMeshes.Add(tracked.VersionHash);
                }).Run();

            Entities
                .WithName("RemoveComponents")
                .WithNone<DeformedEntity>()
                .WithStructuralChanges()
                .WithEntityQueryOptions(EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((Entity e, in SharedMeshTracker tracked) =>
                {
                    EntityManager.RemoveComponent<SharedMeshTracker>(e);
                    EntityManager.RemoveComponent<DeformedMeshIndex>(e);

                    // These components may or may not exist.
                    EntityManager.RemoveComponent<BlendWeightBufferIndex>(e);
                    EntityManager.RemoveComponent<SkinMatrixBufferIndex>(e);

                    rmvMeshes.Add(tracked.VersionHash);
                }).Run();

            var brgRenderMeshArrays = World.GetExistingSystemManaged<RegisterMaterialsAndMeshesSystem>()?.BRGRenderMeshArrays ?? new NativeParallelHashMap<int, BRGRenderMeshArray>();
            var renderMeshArrayHandle = GetSharedComponentTypeHandle<RenderMeshArray>();

            Entities
                .WithName("AddComponents")
                .WithAll<DeformedEntity>()
                .WithNone<SharedMeshTracker>()
                .WithStructuralChanges()
                .ForEach((Entity e, in MaterialMeshInfo materialMeshInfo) =>
                {
                    // If the chunk has a RenderMeshArray, get access to the corresponding registered
                    // Material and Mesh IDs
                    BRGRenderMeshArray brgRenderMeshArray = default;
                    if (!brgRenderMeshArrays.IsEmpty)
                    {
                        int renderMeshArrayIndex = EntityManager.GetChunk(e).GetSharedComponentIndex(renderMeshArrayHandle);
                        bool hasRenderMeshArray = renderMeshArrayIndex >= 0;
                        if (hasRenderMeshArray)
                            brgRenderMeshArrays.TryGetValue(renderMeshArrayIndex, out brgRenderMeshArray);
                    }

                    BatchMeshID meshID = materialMeshInfo.IsRuntimeMesh
                            ? materialMeshInfo.MeshID
                            : brgRenderMeshArray.GetMeshID(materialMeshInfo);

                    if (meshID == BatchMeshID.Null)
                        return;

                    // Mesh is already registered.
                    if (TryGetSharedMeshData(meshID, out var data))
                    {
                        addMeshes.Add(data.StateHash());
                    }
                    // Mesh is not yet registered, register it.
                    else
                    {
                        UnityEngine.Mesh mesh = m_RendererSystem.GetMesh(meshID);
                        Assert.IsNotNull(mesh);

                        // Check the layout of the mesh if it is not valid remove the
                        // deformed mesh component so it will be treated as a regular mesh.
                        if (!HasSupportedVertexLayout(mesh))
                        {
                            EntityManager.RemoveComponent<DeformedEntity>(e);
                            return;
                        }

                        // Note that this do not use 'addMeshes' instead the
                        // shared mesh is initialized with a ref count of 1
                        data = AddSharedMeshData(in meshID, mesh, meshIDs, meshData);
                    }

                    EntityManager.AddComponentData(e, new SharedMeshTracker { VersionHash = data.StateHash() });
                    EntityManager.AddComponent<DeformedMeshIndex>(e);

                    if (data.HasBlendShapes)
                        EntityManager.AddComponent<BlendWeightBufferIndex>(e);

                    if (data.HasSkinning)
                        EntityManager.AddComponent<SkinMatrixBufferIndex>(e);
                }).Run();

            // Update reference counting if a change has occured
            if (rmvMeshes.Length > 0 || addMeshes.Length > 0)
            {
                Job.WithName("UpdateSharedMeshRefCounts")
                   .WithCode(() =>
                   {
                       rmvMeshes.Sort();
                       addMeshes.Sort();

                       // Single pass O(n) in reverse. Both arrays are guaranteed to be sorted.
                       for (int i = meshData.Length - 1, j = rmvMeshes.Length - 1, k = addMeshes.Length - 1; i >= 0 && (j >= 0 || k >= 0); i--)
                       {
                           var hash = meshData[i].StateHash();

                           // - Removal -
                           // 1. Update indexer for current array
                           while (j >= 0 && rmvMeshes[j] > hash)
                               j--;

                           // 2. If this mesh was removed, count how many
                           int rmvCnt = 0;
                           if (j >= 0 && rmvMeshes[j] == hash)
                           {
                               int start = j;

                               while (j > 0 && rmvMeshes[j - 1] == hash)
                                   j--;

                               rmvCnt = start - j + 1;
                           }

                           // - Addition -
                           // 1. Update indexer for current array
                           while (k >= 0 && addMeshes[k] > hash)
                               k--;

                           // 2. If this mesh was added, count how many
                           int addCnt = 0;
                           if (k >= 0 && addMeshes[k] == hash)
                           {
                               int start = k;

                               while (k > 0 && addMeshes[k - 1] == hash)
                                   k--;

                               addCnt = start - k + 1;
                           }

                           int delta = addCnt - rmvCnt;

                           // We can skip updating the ref count if the total number won't change.
                           // Note: we do not need to check for eviction here either because the first
                           // occurance of a mesh does not go through 'addMeshes'. In other words,
                           // the delta will be -1 when all instances are added and removed in the same frame.
                           if (delta != 0)
                           {
                               ref var data = ref meshData.ElementAt(i);
                               data.RefCount += delta;

                               Assert.IsTrue(data.RefCount >= 0);

                               // Evict Mesh if no one references it
                               if (data.RefCount == 0)
                               {
                                   meshData.RemoveAt(i);
                                   meshIDs.RemoveAt(i);
                               }
                           }
                       }
                   }).Run();
            }

            rmvMeshes.Dispose();
            addMeshes.Dispose();

            k_ChangesMarker.End();
        }

        private void LayoutOutputBuffer()
        {
            k_CollectActiveMeshes.Begin();

            var meshes = m_MeshIDs;
            var count = new NativeArray<int>(meshes.Length, Allocator.TempJob);
            var sharedMeshes = m_SharedMeshData;

            // For now assume every mesh is visible & active.
            Dependency = Job
                .WithName("CountActiveMeshes")
                .WithReadOnly(meshes).WithReadOnly(sharedMeshes)
                .WithCode(() =>
                {
                    for (int i = 0; i < sharedMeshes.Length; i++)
                    {
                        var sharedMesh = sharedMeshes[i];
                        var meshId = sharedMesh.MeshID;
                        var index = meshes.IndexOf(meshId);
                        count[index] = sharedMesh.RefCount;
                    }
                }).Schedule(Dependency);

            //Dependency = Entities
            //    .WithName("CountActiveMeshes")
            //    .WithAll<DeformedEntity>().WithAll<SharedMeshTracker>()
            //    .WithReadOnly(meshes)
            //    .WithNativeDisableParallelForRestriction(count)
            //    .ForEach((in MaterialMeshInfo id) =>
            //    {
            //        var meshID = id.MeshID;

            //        if (meshID == BatchMeshID.Null)
            //            return;

            //        var index = meshes.IndexOf(meshID);

            //        // For now assume every mesh is visible & active.

            //        unsafe
            //        {
            //            Interlocked.Increment(ref UnsafeUtility.ArrayElementAsRef<int>(count.GetUnsafePtr(), index));
            //        }
            //    }).ScheduleParallel(Dependency);

            k_CollectActiveMeshes.End();
            k_OutputCountBuffer.Begin();

            var vertexCountRef = m_MeshVertexCount;
            var shapeWeightCountRef = m_BlendShapeWeightCount;
            var skinMatrixCountRef = m_SkinMatrixCount;
            var batches = DeformationBatches;
            batches.Clear();

            Dependency = Job
                .WithName("ConstructDeformationBatches")
                .WithReadOnly(meshes).WithReadOnly(sharedMeshes)
                .WithCode(() =>
            {
                int vertexCount, shapeWeightCount, skinMatrixCount;
                vertexCount = shapeWeightCount = skinMatrixCount = k_DeformBufferStartIndex;

                for (int i = 0; i < meshes.Length; i++)
                {
                    var instanceCount = count[i];

                    // Skip this mesh if we do not have any active instances.
                    if (instanceCount == 0)
                        continue;

                    var id = meshes[i];
                    var batch = new MeshDeformationBatch
                    {
                        BatchIndex = batches.Count(),
                        MeshVertexIndex = vertexCount,
                        BlendShapeIndex = shapeWeightCount,
                        SkinMatrixIndex = skinMatrixCount,
                        InstanceCount = instanceCount,
                    };
                    batches.Add(id, batch);

                    var meshData = sharedMeshes[meshes.IndexOf(id)];

                    vertexCount += instanceCount * meshData.VertexCount;

                    if (meshData.HasBlendShapes)
                        shapeWeightCount += instanceCount * meshData.BlendShapeCount;

                    if (meshData.HasSkinning)
                        skinMatrixCount += instanceCount * meshData.BoneCount;
                }

                // Assign the total counts
                vertexCountRef.Value = vertexCount;
                shapeWeightCountRef.Value = shapeWeightCount;
                skinMatrixCountRef.Value = skinMatrixCount;
            }).Schedule(Dependency);

            m_BatchConstructionHandle = Dependency;

            var brgRenderMeshArrays = World.GetExistingSystemManaged<RegisterMaterialsAndMeshesSystem>()?.BRGRenderMeshArrays ?? new NativeParallelHashMap<int, BRGRenderMeshArray>();
            var renderMeshArrayHandle = GetSharedComponentTypeHandle<RenderMeshArray>();

            Dependency = new LayoutDeformedMeshJob
            {
                DeformedMeshIndexHandle = GetComponentTypeHandle<DeformedMeshIndex>(),
                BlendWeightBufferIndexHandle = GetComponentTypeHandle<BlendWeightBufferIndex>(),
                SkinMatrixBufferIndexHandle = GetComponentTypeHandle<SkinMatrixBufferIndex>(),
                RenderMeshArrayHandle = renderMeshArrayHandle,
                MaterialMeshInfoHandle = GetComponentTypeHandle<MaterialMeshInfo>(),
                BatchData = DeformationBatches,
                BRGRenderMeshArrays = brgRenderMeshArrays,
                SharedMeshData = m_SharedMeshData.AsArray(),
                MeshIDs = m_MeshIDs.AsArray(),
                MeshCounts = count,
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
                DeformedMeshBufferIndex = m_MeshBufferManager.ActiveDeformedMeshBufferIndex,
#endif
            }.ScheduleParallel(m_LayoutDeformedMeshesQuery, Dependency);

            Dependency = count.Dispose(Dependency);

            k_OutputCountBuffer.End();
        }

        private void FitOutputBuffer()
        {
            k_OutputResizeBuffer.Begin();
            SkinningBufferManager.ResizePassBufferIfRequired(m_SkinMatrixCount.Value);
            BlendShapeBufferManager.ResizePassBufferIfRequired(m_BlendShapeWeightCount.Value);
            m_MeshBufferManager.ResizeAndPushDeformMeshBuffersIfRequired(m_MeshVertexCount.Value);
            k_OutputResizeBuffer.End();
        }

        [BurstCompile]
        struct LayoutDeformedMeshJob : IJobChunk
        {
            public ComponentTypeHandle<DeformedMeshIndex> DeformedMeshIndexHandle;
            public ComponentTypeHandle<BlendWeightBufferIndex> BlendWeightBufferIndexHandle;
            public ComponentTypeHandle<SkinMatrixBufferIndex> SkinMatrixBufferIndexHandle;

            [ReadOnly] public SharedComponentTypeHandle<RenderMeshArray> RenderMeshArrayHandle;
            [ReadOnly] public ComponentTypeHandle<MaterialMeshInfo> MaterialMeshInfoHandle;

            [NativeDisableContainerSafetyRestriction] public NativeArray<int> MeshCounts;

            [ReadOnly] public NativeParallelHashMap<BatchMeshID, MeshDeformationBatch> BatchData;
            [ReadOnly] public NativeParallelHashMap<int, BRGRenderMeshArray> BRGRenderMeshArrays;
            [ReadOnly] public NativeArray<SharedMeshData> SharedMeshData;
            [ReadOnly] public NativeArray<BatchMeshID> MeshIDs;
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
            [ReadOnly] public int DeformedMeshBufferIndex;
#endif

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 enabledMask)
            {
                Assert.IsFalse(useEnabledMask);

                var meshIndices = chunk.GetNativeArray(ref DeformedMeshIndexHandle);
                var blendWeightIndices = chunk.GetNativeArray(ref BlendWeightBufferIndexHandle);
                var skinMatrixIndices = chunk.GetNativeArray(ref SkinMatrixBufferIndexHandle);
                var meshInfos = chunk.GetNativeArray(ref MaterialMeshInfoHandle);

                // If the chunk has a RenderMeshArray, get access to the corresponding registered
                // Material and Mesh IDs
                BRGRenderMeshArray brgRenderMeshArray = default;
                if (!BRGRenderMeshArrays.IsEmpty)
                {
                    int renderMeshArrayIndex = chunk.GetSharedComponentIndex(RenderMeshArrayHandle);
                    bool hasRenderMeshArray = renderMeshArrayIndex >= 0;
                    if (hasRenderMeshArray)
                        BRGRenderMeshArrays.TryGetValue(renderMeshArrayIndex, out brgRenderMeshArray);
                }

                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
                {
                    var materialMeshInfo = meshInfos[i];

                    var meshID = materialMeshInfo.IsRuntimeMesh
                        ? materialMeshInfo.MeshID
                        : brgRenderMeshArray.GetMeshID(materialMeshInfo);

                    if (meshID == BatchMeshID.Null)
                        continue;

                    var batchRange = BatchData[meshID];
                    var meshIndex = MeshIDs.IndexOf(meshID);
                    var meshData = SharedMeshData[meshIndex];

                    unsafe
                    {
                        var instanceIndex =
                            Interlocked.Decrement(
                                ref UnsafeUtility.ArrayElementAsRef<int>(MeshCounts.GetUnsafePtr(), meshIndex));

                        uint deformedMeshIndex =
                            (uint)(batchRange.MeshVertexIndex + instanceIndex * meshData.VertexCount);

#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
                        ref var component =
 ref UnsafeUtility.ArrayElementAsRef<DeformedMeshIndex>(meshIndices.GetUnsafePtr(), i);
                        // Set index into deformed mesh buffer for the current frame
                        component.Value[DeformedMeshBufferIndex] = deformedMeshIndex;
                        // Set current frame buffer index (0 or 1)
                        component.Value[2] = (uint)DeformedMeshBufferIndex;
#else
                        meshIndices[i] = new DeformedMeshIndex { Value = deformedMeshIndex };
#endif

                        if (meshData.HasBlendShapes)
                        {
                            Assert.IsTrue(blendWeightIndices.IsCreated);
                            blendWeightIndices[i] = new BlendWeightBufferIndex
                                { Value = batchRange.BlendShapeIndex + instanceIndex * meshData.BlendShapeCount };
                        }

                        if (meshData.HasSkinning)
                        {
                            Assert.IsTrue(skinMatrixIndices.IsCreated);
                            skinMatrixIndices[i] = new SkinMatrixBufferIndex
                                { Value = batchRange.SkinMatrixIndex + instanceIndex * meshData.BoneCount };
                        }
                    }
                }
            }
        }
    }
}
