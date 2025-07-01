using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.VFX {
    public class DepthCustomPass : CustomPass {
        // !-- From UnityEngine.Rendering.HighDefinition.CustomPassUtils
        static ProfilingSampler renderDepthFromCameraSampler = new ProfilingSampler("Render Depth");

        static ProfilingSampler renderFromCameraSampler = new ProfilingSampler("Render From Camera");

        // !-- From UnityEngine.Rendering.HighDefinition.CustomPassUtils
        public RenderTexture depthTexture = null;
        protected override bool executeInSceneView => false;

        // !-- From UnityEngine.Rendering.HighDefinition.CustomPassUtils
        ShaderTagId[] depthTags;
        Material customPassRenderersUtilsMaterial;
        int depthPassIndex;

        [UnityEngine.Scripting.Preserve]
        public DepthCustomPass() { }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {
            depthTags = new[] { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
            customPassRenderersUtilsMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/HDRP/CustomPassRenderersUtils"));
            depthPassIndex = customPassRenderersUtilsMaterial.FindPass("DepthPass");
        }

        protected override void Cleanup() {
            if (customPassRenderersUtilsMaterial) {
                GameObjects.DestroySafely(customPassRenderersUtilsMaterial);
            }
        }
        // !-- From UnityEngine.Rendering.HighDefinition.CustomPassUtils

        protected override void Execute(CustomPassContext ctx) {
            if (depthTexture == null) {
                return;
            }
            var camera = ctx.hdCamera.camera;
            camera.TryGetCullingParameters(out var cullingParams);
            cullingParams.cullingOptions = CullingOptions.None;
            cullingParams.shadowDistance = 0;
            cullingParams.maximumVisibleLights = 0;
            cullingParams.accurateOcclusionThreshold = 0;

            ctx.cullingResults = ctx.renderContext.Cull(ref cullingParams);
            var overrideDepthTest = new RenderStateBlock(RenderStateMask.Depth) {
                depthState = new(true, CompareFunction.LessEqual),
            };

            RenderDepthFromCamera(ctx, depthTexture, ClearFlag.Depth,
                camera.cullingMask, RenderQueueType.AllOpaque, overrideDepthTest);
        }

        // !-- From UnityEngine.Rendering.HighDefinition.CustomPassUtils
        void RenderDepthFromCamera(in CustomPassContext ctx, RenderTexture targetRenderTexture, ClearFlag clearFlag, LayerMask layerMask, RenderQueueType renderQueueFilter = RenderQueueType.All, RenderStateBlock overrideRenderState = default) {
            using var scope = new ProfilingScope(ctx.cmd, renderDepthFromCameraSampler);
            RenderFromCamera(ctx, targetRenderTexture, clearFlag, layerMask, renderQueueFilter, customPassRenderersUtilsMaterial, depthPassIndex, overrideRenderState);
        }

        void RenderFromCamera(in CustomPassContext ctx, RenderTexture targetRenderTexture, ClearFlag clearFlag, LayerMask layerMask, RenderQueueType renderQueueFilter = RenderQueueType.All, Material overrideMaterial = null, int overrideMaterialIndex = 0, RenderStateBlock overrideRenderState = default) {
            CoreUtils.SetRenderTarget(ctx.cmd, targetRenderTexture.colorBuffer, targetRenderTexture.depthBuffer, clearFlag);
            using var disableSinglePassRendering = new CustomPassUtils.DisableSinglePassRendering(ctx);
            using var scope = new ProfilingScope(ctx.cmd, renderFromCameraSampler);
            DrawRenderers(ctx, layerMask, renderQueueFilter, overrideMaterial, overrideMaterialIndex, overrideRenderState);
        }

        void DrawRenderers(in CustomPassContext ctx, LayerMask layerMask, RenderQueueType renderQueueFilter = RenderQueueType.All, Material overrideMaterial = null, int overrideMaterialIndex = 0, RenderStateBlock overrideRenderState = default, SortingCriteria sorting = SortingCriteria.CommonOpaque) {
            // In UnityEngine.Rendering.HighDefinition.CustomPassUtils there is a bug, that they are using litForwardTags
            // so here I fixed it by using depthTags, because we are drawing depth
            CustomPassUtils.DrawRenderers(ctx, depthTags, layerMask, renderQueueFilter, overrideMaterial,
                overrideMaterialIndex, overrideRenderState, sorting);
        }
        // !-- From UnityEngine.Rendering.HighDefinition.CustomPassUtils
    }
}