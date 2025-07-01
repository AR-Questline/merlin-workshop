#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Unity.Rendering.Occlusion.Masked.Dots;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Rendering.Occlusion.Masked.Visualization
{
    /* This class stores the resources needed to draw debug visualizations for any view that uses occlusion culling.
       Views can be cameras, lights' shadowmaps, reflection probes, or anything else that renders the scene.

       A debug view is allocated only when a debug visualization is requested by the user. Otherwise no memory is
       allocated for debug purposes. */
    unsafe class DebugView
    {
        static readonly int s_DepthPropertyID = Shader.PropertyToID("_Depth");
        static readonly int s_OverlayPropertyID = Shader.PropertyToID("_Overlay");
        static readonly int s_YFlipPropertyID = Shader.PropertyToID("_YFlip");
        static readonly int s_TransformPropertyID = Shader.PropertyToID("_Transform");
        static readonly int s_OnlyOverlayPropertyID = Shader.PropertyToID("_OnlyOverlay");
        static readonly int s_OnlyDepthPropertyID = Shader.PropertyToID("_OnlyDepth");
        static readonly Shader s_CompositeShader = Shader.Find("Hidden/OcclusionDebugComposite");
        static readonly Shader s_OccluderShader = Shader.Find("Hidden/OcclusionDebugOccluders");

        static readonly ProfilerMarker s_MaskedDepthToPixelDepth = new ProfilerMarker("Occlusion.Debug.RenderView.MaskedDepthToPixelDepth");
        static readonly ProfilerMarker s_OcclusionTests = new ProfilerMarker("Occlusion.Debug.RenderView.OcclusionTests");
        static readonly ProfilerMarker s_GenerateAABBMesh = new ProfilerMarker("Occlusion.Debug.RenderView.GenerateAABBMesh");
        static readonly ProfilerMarker s_GenerateOutlineMesh = new ProfilerMarker("Occlusion.Debug.RenderView.GenerateOutlineMesh");
        static readonly ProfilerMarker s_GenerateOccluderMesh = new ProfilerMarker("Occlusion.Debug.RenderView.GenerateOccluderMesh");

        static readonly CommandBuffer s_CmdLayers = new CommandBuffer() { name = "Occlusion debug layers" };
        Material s_OccludeeAABBsMaterial;

        // Memory used to upload CPU depth to GPU
        NativeArray<float> m_CPUDepth;
        public Texture2D gpuDepth;
        // Debug texture drawn to the screen as an overlay
        RenderTexture m_Overlay;
        // Single mesh containing AABBs of all culled occludees
        Mesh m_OccludeeAabBsMesh;
        Material m_OccluderMaterial;
        // Single mesh containing all the occluder meshes
        Mesh m_OccludersMesh;
        VertexAttributeDescriptor[] m_OccluderVertexLayout;
        // Final composite
        Material m_CompositeMaterial;

        private bool AnyMeshOrMaterialNull()
        {
            return m_OccludeeAabBsMesh == null || m_OccluderMaterial == null || m_OccludersMesh == null || m_CompositeMaterial == null || s_OccludeeAABBsMaterial == null;
        }
        private void CreateMeshAndMaterials()
        {
            m_OccludeeAabBsMesh = new Mesh();
            m_OccludersMesh = new Mesh();
            m_OccluderMaterial = new Material(s_OccluderShader);
            m_OccluderMaterial.SetPass(0);
            m_CompositeMaterial = new Material(s_CompositeShader);
            m_CompositeMaterial.SetPass(0);
            m_OccluderVertexLayout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, stream: 1)
            };
            s_OccludeeAABBsMaterial = new Material(Shader.Find("Hidden/OccludeeScreenSpaceAABB"));
        }
        public DebugView()
        {
        }

        public void Dispose()
        {
            m_CPUDepth.Dispose();
            Object.DestroyImmediate(gpuDepth);
            Object.DestroyImmediate(m_Overlay);
        }

        // Ensure that the member resources fit the dimensions of the view
        public void ReallocateIfNeeded(int viewWidth, int viewHeight)
        {
            if (AnyMeshOrMaterialNull())
            {
                CreateMeshAndMaterials();
            }

            // Reallocate depth texture if needed
            if (!gpuDepth || gpuDepth.width != viewWidth || gpuDepth.height != viewHeight)
            {
                if (gpuDepth)
                {
                    Object.DestroyImmediate(gpuDepth);
                }
                gpuDepth = new Texture2D(viewWidth, viewHeight, TextureFormat.RFloat, false);
                gpuDepth.filterMode = FilterMode.Point;
                m_CompositeMaterial.SetTexture(s_DepthPropertyID, gpuDepth);
            }
            // Reallocate overlay texture if needed
            if (!m_Overlay || m_Overlay.width != viewWidth || m_Overlay.height != viewHeight)
            {
                if (m_Overlay)
                {
                    Object.DestroyImmediate(m_Overlay);
                }
                m_Overlay = new RenderTexture(viewWidth, viewHeight, 1, RenderTextureFormat.RFloat, 0);
                m_CompositeMaterial.SetTexture(s_OverlayPropertyID, m_Overlay);
            }
            // Reallocate cpu-side debug depth buffer if needed
            if (!m_CPUDepth.IsCreated || m_CPUDepth.Length != viewWidth * viewHeight)
            {
                if (m_CPUDepth.IsCreated)
                {
                    m_CPUDepth.Dispose();
                }
                m_CPUDepth = new NativeArray<float>(viewWidth * viewHeight, Allocator.Persistent);
            }
        }

        // Render to the depth buffer and the overlay texture
        public void RenderToTextures(
            EntityQuery testQuery,
            EntityQuery meshQuery,
            BufferGroup bufferGroup,
            DebugRenderMode mode
#if UNITY_EDITOR
            , bool isOcclusionBrowseWindowVisible
#endif
        )
        {
            if (AnyMeshOrMaterialNull())
            {
                CreateMeshAndMaterials();
            }
            s_CmdLayers.Clear();
            // Write the CPU-rasterized depth buffer to a GPU texture, and then blit it to the overlay
            if (mode == DebugRenderMode.Depth ||
                mode == DebugRenderMode.Test
#if UNITY_EDITOR
                || isOcclusionBrowseWindowVisible
#endif
                )
            {
                s_MaskedDepthToPixelDepth.Begin();
                int width = bufferGroup.NumPixelsX;
                int height = bufferGroup.NumPixelsY;
                int numTilesX = bufferGroup.NumTilesX;
                int numTilesY = bufferGroup.NumTilesY;
                var job = new DecodeMaskedDepthJob()
                {
                    // In
                    NumPixelsX = width,
                    NumPixelsY = height,
                    NumTilesX = numTilesX,
                    Tiles = (Tile*)bufferGroup.Tiles.GetUnsafeReadOnlyPtr(),
                    // Out
                    DecodedZBuffer = m_CPUDepth,
                };
                job.Schedule((numTilesX * numTilesY), 64).Complete();

                gpuDepth.SetPixelData(m_CPUDepth, 0);
                gpuDepth.Apply();
                s_MaskedDepthToPixelDepth.End();
            }

            if (mode == DebugRenderMode.Test)
            {
                s_OcclusionTests.Begin();

                // Extract AABBs which are tested and culled due to occlusion culling
                NativeArray<OcclusionTest> allTests = testQuery.ToComponentDataArray<OcclusionTest>(Allocator.TempJob);
                var culledTestsQueue = new NativeQueue<OcclusionTest>(Allocator.TempJob);

                var testJob = new FilterOccludedTestJob()
                {
                    // In
                    ProjectionType = bufferGroup.ProjectionType,
                    NumTilesX = bufferGroup.NumTilesX,
                    HalfSize = bufferGroup.HalfSize,
                    PixelCenter = bufferGroup.PixelCenter,
                    ScreenSize = bufferGroup.ScreenSize,
                    Tiles = (Tile*)bufferGroup.Tiles.GetUnsafeReadOnlyPtr(),
                    AllTests = allTests,
                    // Out
                    culledTestsQueue = culledTestsQueue.AsParallelWriter(),
                };
                testJob.Schedule(allTests.Length, 1).Complete();
                var culledTests = culledTestsQueue.ToArray(Allocator.TempJob);
                culledTestsQueue.Dispose();
                allTests.Dispose();
                s_OcclusionTests.End();

                s_GenerateAABBMesh.Begin();
                // Create a mesh with an AABB for every culled occludee
                {
                    var dataArray = Mesh.AllocateWritableMeshData(1);
                    var data = dataArray[0];
                    // We need 4 verts and 6 indices per AABB
                    data.SetVertexBufferParams(4 * culledTests.Length, new VertexAttributeDescriptor(VertexAttribute.Position));
                    data.SetIndexBufferParams(6 * culledTests.Length, IndexFormat.UInt16);
                    NativeArray<Vector3> verts = data.GetVertexData<Vector3>();
                    NativeArray<ushort> indices = data.GetIndexData<ushort>();
                    // Fill the mesh data in a job
                    var job = new OccludeeAABBJob()
                    {
                        // In
                        CulledTests = culledTests,
                        // Out
                        Verts = verts,
                        Indices = indices,
                    };
                    job.Schedule(culledTests.Length, 32).Complete();
                    data.subMeshCount = 1;
                    data.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length));
                    // Create the mesh and apply data to it:
                    Mesh.ApplyAndDisposeWritableMeshData(dataArray, m_OccludeeAabBsMesh);
                    m_OccludeeAabBsMesh.RecalculateBounds();
                }

                // Draw to the overlay
                s_CmdLayers.SetRenderTarget(m_Overlay);
                s_CmdLayers.ClearRenderTarget(false, true, Color.clear);
                s_CmdLayers.DrawMesh(m_OccludeeAabBsMesh, Matrix4x4.identity, s_OccludeeAABBsMaterial);
                culledTests.Dispose();
                s_GenerateAABBMesh.End();
            }
            else if (mode == DebugRenderMode.Bounds)
            {
                s_GenerateOutlineMesh.Begin();
                // Create a mesh with an AABB for each occludee, regardless of whether it is culled or not
                {
                    NativeArray<OcclusionTest> allTests = testQuery.ToComponentDataArray<OcclusionTest>(Allocator.TempJob);

                    var dataArray = Mesh.AllocateWritableMeshData(1);
                    var data = dataArray[0];
                    // We need 4 verts and 6 indices per AABB
                    data.SetVertexBufferParams(16 * allTests.Length, new VertexAttributeDescriptor(VertexAttribute.Position));
                    data.SetIndexBufferParams(24 * allTests.Length, IndexFormat.UInt32);
                    NativeArray<Vector3> verts = data.GetVertexData<Vector3>();
                    NativeArray<uint> indices = data.GetIndexData<uint>();
                    // Fill the mesh data in a job
                    var job = new OccludeeOutlineJob()
                    {
                        // In
                        InvResolution = new float2(1f / m_Overlay.width, 1f / m_Overlay.height),
                        AllTests = allTests,
                        // Out
                        Verts = verts,
                        Indices = indices,
                    };
                    job.Schedule(allTests.Length, 32).Complete();
                    data.subMeshCount = 1;
                    data.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length));
                    // Create the mesh and apply data to it:
                    Mesh.ApplyAndDisposeWritableMeshData(dataArray, m_OccludeeAabBsMesh);
                    m_OccludeeAabBsMesh.RecalculateBounds();
                    allTests.Dispose();
                }
                s_CmdLayers.SetRenderTarget(m_Overlay);
                s_CmdLayers.ClearRenderTarget(false, true, Color.clear);
                s_CmdLayers.DrawMesh(m_OccludeeAabBsMesh, Matrix4x4.identity, s_OccludeeAABBsMaterial);
                s_GenerateOutlineMesh.End();
            }
            else if (mode == DebugRenderMode.Mesh)
            {
                s_GenerateOccluderMesh.Begin();
                NativeArray<OcclusionMesh> meshes = meshQuery.ToComponentDataArray<OcclusionMesh>(Allocator.TempJob);
                NativeArray<LocalToWorld> localToWorlds = meshQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
                // To parallelize mesh aggregation, we first need to find the total number of vertices and indices
                // across all occluder meshes. We also need to precompute the offsets of each occluder mesh in the
                // aggregated vertex and index buffers.
                var vertOffsets = new NativeArray<int>(meshes.Length, Allocator.TempJob);
                var indexOffsets = new NativeArray<int>(meshes.Length, Allocator.TempJob);
                int numVerts = 0;
                int numIndices = 0;
                {
                    for (int i = 0; i < meshes.Length; i++)
                    {
                        vertOffsets[i] = numVerts;
                        numVerts += meshes[i].vertexCount;
                    }

                    for (int i = 0; i < meshes.Length; i++)
                    {
                        indexOffsets[i] = numIndices;
                        numIndices += meshes[i].indexCount;
                    }
                }

                // Prepare a single mesh containing all of the occluders
                {
                    var dataArray = Mesh.AllocateWritableMeshData(1);
                    var data = dataArray[0];
                    data.SetVertexBufferParams(numVerts, m_OccluderVertexLayout);
                    // We use a 32-bit index buffer to handle large meshes.
                    data.SetIndexBufferParams(numIndices, IndexFormat.UInt32);
                    NativeArray<Vector4> verts = data.GetVertexData<Vector4>();
                    NativeArray<Color32> colors = data.GetVertexData<Color32>(stream: 1);
                    NativeArray<int> indices = data.GetIndexData<int>();
                    // Fill the mesh data in a job
                    new MeshAggregationJob()
                        {
                            // In
                            Meshes = meshes,
                            LocalToWorlds = localToWorlds,
                            VertOffsets = vertOffsets,
                            IndexOffsets = indexOffsets,
                            // Out
                            Verts = verts,
                            Colors = colors,
                            Indices = indices,
                        }.Schedule(meshes.Length, 4)
                        .Complete();
                    data.subMeshCount = 1;
                    data.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length));
                    // Create the mesh and apply data to it:
                    Mesh.ApplyAndDisposeWritableMeshData(dataArray, m_OccludersMesh);
                    // the vertices are already in screenspace and perspective projected
                    m_OccludersMesh.bounds = new Bounds(new Vector3(-1000, -1000, -1000), new Vector3(10000, 10000, 1000));
                }

                vertOffsets.Dispose();
                indexOffsets.Dispose();
                meshes.Dispose();
                localToWorlds.Dispose();
                s_GenerateOccluderMesh.End();
            }
            Graphics.ExecuteCommandBuffer(s_CmdLayers);
        }

        public void RenderToCamera(DebugRenderMode renderMode, Camera camera, CommandBuffer cmd, Mesh fullScreenQuad, Matrix4x4 cullingMatrix)
        {
            if (renderMode == DebugRenderMode.None)
                return;

            float yFlip = 0f;
#if UNITY_EDITOR
            if(camera.cameraType == CameraType.Preview)
            {
                yFlip = 1f;
            }
            if (Camera.current != null && camera == SceneView.currentDrawingSceneView.camera)
            {
                yFlip = 1f;
            }
#endif // UNITY_EDITOR

            if (renderMode == DebugRenderMode.Mesh)
            {
                var material = m_OccluderMaterial;
                material.SetFloat(s_YFlipPropertyID, yFlip);
                material.SetMatrix(s_TransformPropertyID, cullingMatrix);
                cmd.DrawMesh(m_OccludersMesh, Matrix4x4.identity, material);
            }
            else
            {
                m_CompositeMaterial.SetFloat(s_YFlipPropertyID, yFlip);
                m_CompositeMaterial.SetFloat(s_OnlyOverlayPropertyID, (renderMode == DebugRenderMode.Bounds) ? 1f : 0f);
                m_CompositeMaterial.SetFloat(s_OnlyDepthPropertyID, (renderMode == DebugRenderMode.Depth) ? 1f : 0f);
                cmd.DrawMesh(fullScreenQuad, Matrix4x4.identity, m_CompositeMaterial);
            }
        }
    }
}

#endif
