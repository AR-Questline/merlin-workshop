#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Rendering.Occlusion.Masked;
using Unity.Rendering.Occlusion.Masked.Visualization;

namespace Unity.Rendering.Occlusion
{

    /* This system renders debug visualizations for occlusion culling.
       For each debug view:
       1. It unpacks the CPU-rasterized masked depth buffer into a human-readable depth buffer
       2. It renders the visualization (such as culled occludees, or occluder meshes) into an overlay texture
       3. At the end of every frame, it composites these textures to create the final debug visualization */
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(EntitiesGraphicsSystem))]
   partial class OcclusionDebugRenderSystem : SystemBase
   {
        private EntitiesGraphicsSystem entitiesGraphicsSystem;
        private CommandBuffer cmdComposite = new CommandBuffer() { name = "Occlusion debug composite" };
        private Mesh fullScreenQuad;

        struct QuadVertex
        {
            public float4 pos;
            public float2 uv;
        }

        protected void SetFullScreenQuad()
        {

            var layout = new[]
            {
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };

            fullScreenQuad = new Mesh();
            fullScreenQuad.SetVertexBufferParams(4, layout);

            var verts = new NativeArray<QuadVertex>(4, Allocator.Temp);

            verts[0] = new QuadVertex() { pos = new float4(-1f, -1f, 1, 1), uv = new float2(0, 1) };
            verts[1] = new QuadVertex() { pos = new float4(1f, -1f, 1, 1), uv = new float2(1, 1) };
            verts[2] = new QuadVertex() { pos = new float4(1f, 1f, 1, 1), uv = new float2(1, 0) };
            verts[3] = new QuadVertex() { pos = new float4(-1f, 1f, 1, 1), uv = new float2(0, 0) };

            fullScreenQuad.SetVertexBufferData(verts, 0, 0, 4);
            verts.Dispose();

            var tris = new int[6] { 0, 1, 2, 0, 2, 3 };
            fullScreenQuad.SetIndices(tris, MeshTopology.Triangles, 0);
            fullScreenQuad.bounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 1000));
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            // Create full-screen quad
            SetFullScreenQuad();

            RenderPipelineManager.endFrameRendering += RenderComposite;
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            RenderPipelineManager.endFrameRendering -= RenderComposite;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {

        }

        // At the end of every frame, composite the depth and overlay texture for each view to create the final debug
        // visualization
        void RenderComposite(ScriptableRenderContext ctx, Camera[] cameras)
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            if (entitiesGraphicsSystem == null)
            {
                entitiesGraphicsSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            }

            OcclusionCulling oc = entitiesGraphicsSystem.OcclusionCulling;

            if (!oc.IsEnabled ||
                oc.debugSettings.debugRenderMode == DebugRenderMode.None ||
                oc.debugSettings.debugRenderMode == DebugRenderMode.Inverted)
                return;

            // This happens when reloading scenes
            if(fullScreenQuad == null)
            {
                SetFullScreenQuad();
            }

            cmdComposite.Clear();
            
            ulong? pinnedViewID = oc.debugSettings.GetPinnedViewID();
            
            foreach (Camera camera in cameras)
            {
                ulong id = pinnedViewID.HasValue ?
                    // A view is pinned. Pick its ID instead of the current camera's ID.
                    pinnedViewID.Value :
                    // No view is pinned. Pick the current camera's ID.
                    (ulong) ((uint)camera.GetInstanceID());
                if (!oc.BufferGroups.TryGetValue(id, out BufferGroup bufferGroup))
                    continue;
                bufferGroup.RenderToCamera(oc.debugSettings.debugRenderMode, camera, cmdComposite, fullScreenQuad);
            }
            Graphics.ExecuteCommandBuffer(cmdComposite);
        }
    }
}

#endif
