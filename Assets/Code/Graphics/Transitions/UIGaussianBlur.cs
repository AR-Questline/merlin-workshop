using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace Awaken.TG.Graphics.Transitions {
    class UIGaussianBlur : CustomPass {
        
        public LayerMask blurLayerMask; 
        
        [Range(0f, 1f)]
        public float intensity = 1.0f; 
        [Range(0f, 64f)]
        public float maxRadius = 8.0f;
        [Range(2, 64)]
        public int sampleCount = 32;
        
        RTHandle _halfResTarget;

        [UnityEngine.Scripting.Preserve]
        public UIGaussianBlur() {}
        
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {
            _halfResTarget = RTHandles.Alloc(
                Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                useDynamicScale: true, name: "Half Res Custom Pass"
            );
        }

        protected override void Execute(CustomPassContext ctx) {
            var rendererListDesc = new RendererListDesc(new ShaderTagId("HDRPForward"), ctx.cullingResults, ctx.hdCamera.camera) {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.all,
                layerMask = blurLayerMask
            };

            var rendererList = ctx.renderContext.CreateRendererList(rendererListDesc);
            
            ctx.cmd.DrawRendererList(rendererList);

            float scaledRadius = intensity * maxRadius * ctx.cameraColorBuffer.rtHandleProperties.rtHandleScale.x;

            CustomPassUtils.GaussianBlur(
                ctx, ctx.cameraColorBuffer, ctx.cameraColorBuffer, _halfResTarget,
                sampleCount, scaledRadius, downSample: true
            );
        }

        protected override void Cleanup() {
            _halfResTarget.Release();
        }
    }
}