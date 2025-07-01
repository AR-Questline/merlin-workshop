using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RenderGraphModule;
using UniversalProfiling;

namespace Awaken.TG.Graphics.VFX {
    public class ScreenSpaceWetness : CustomPass {
        static readonly int RainIntensity = Shader.PropertyToID("_RainIntensity");
        static readonly int Moisture = Shader.PropertyToID("_Moisture");
        static readonly int DepthTexturesArray = Shader.PropertyToID("_DepthTexturesArray");
        static readonly int DepthTexturesLayers = Shader.PropertyToID("_DepthTexturesLayers");
        static readonly int DepthTexBottomLeftUVOffset = Shader.PropertyToID("_DepthTexBottomLeftUVOffset");
        static readonly int DepthTexBottomRightUVOffset = Shader.PropertyToID("_DepthTexBottomRightUVOffset");
        static readonly int DepthTexTopLeftUVOffset = Shader.PropertyToID("_DepthTexTopLeftUVOffset");
        static readonly int DepthTexTopRightUVOffset = Shader.PropertyToID("_DepthTexTopRightUVOffset");
        static readonly int DepthTexturesUVInvScale = Shader.PropertyToID("_DepthTexturesUVInvScale");
        static readonly int MaxHeightDiff = Shader.PropertyToID("_MaxHeightDiff");

        static readonly int TopDownVp = Shader.PropertyToID("_TopDown_VP");
        static readonly int TopDownV = Shader.PropertyToID("_TopDown_V");
        static readonly int TopDownNearPlane = Shader.PropertyToID("_TopDown_NearPlane");
        static readonly int TopDownFarPlane = Shader.PropertyToID("_TopDown_FarPlane");
        static readonly int TopDownYCamera = Shader.PropertyToID("_TopDown_YCamera");
        static readonly UniversalProfilerMarker ExecuteMarker = new("ScreenSpaceWetness.Execute");
        static readonly UniversalProfilerMarker InitializeMarker = new("ScreenSpaceWetness.SetupBeforeExecute");

        [SerializeField] Material wetnessMaterial;
        
        MaterialPropertyBlock _props;
        TopDownDepthTexturesLoadingManager _depthTexturesLoadingManager;
        TextureHandle _tempNormalBufferHandle;

        [UnityEngine.Scripting.Preserve]
        public ScreenSpaceWetness() { }

        public void SetDepthReferences(TopDownDepthTexturesLoadingManager depthTexturesLoadingManager) {
            this._depthTexturesLoadingManager = depthTexturesLoadingManager;
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {
            _props = new MaterialPropertyBlock();
        }

        public void Update(float rainIntensity, float moisture) {
            if (wetnessMaterial != null && _props != null) {
                _props.SetFloat(RainIntensity, rainIntensity);
                _props.SetFloat(Moisture, moisture);
            }
        }

        public override IEnumerable<Material> RegisterMaterialForInspector() {
            if (wetnessMaterial != null)
                yield return wetnessMaterial;
        }

        protected override void SetupBeforeExecute(RenderGraph renderGraph, RenderGraphBuilder builder, RenderTargets currentRenderTargets) {
            using var marker = InitializeMarker.Auto(); 
            var cameraNormalBufferDescriptor = currentRenderTargets.normalBufferRG.GetDescriptor(renderGraph);
            _tempNormalBufferHandle = renderGraph.CreateTexture(cameraNormalBufferDescriptor);
            builder.ReadWriteTexture(_tempNormalBufferHandle);
        }

        protected override void Execute(CustomPassContext ctx) {
            if (injectionPoint != CustomPassInjectionPoint.AfterOpaqueDepthAndNormal) {
                Debug.LogError(
                    "Custom Pass ScreenSpaceWetness needs to be used at the injection point AfterOpaqueDepthAndNormal.");
                return;
            }

            if (wetnessMaterial == null || _depthTexturesLoadingManager == null || _depthTexturesLoadingManager.IsInitialized == false) {
                return;
            }

            ExecuteMarker.Begin();

            _props.SetTexture(DepthTexturesArray, _depthTexturesLoadingManager.DepthTexturesArray);
            _props.SetVector(DepthTexturesLayers, _depthTexturesLoadingManager.DepthTexturesLayers);
            _props.SetVector(DepthTexBottomLeftUVOffset, (Vector2)_depthTexturesLoadingManager.TexBottomLeftUVOffset);
            _props.SetVector(DepthTexBottomRightUVOffset, (Vector2)_depthTexturesLoadingManager.TexBottomRightUVOffset);
            _props.SetVector(DepthTexTopLeftUVOffset, (Vector2)_depthTexturesLoadingManager.TexTopLeftUVOffset);
            _props.SetVector(DepthTexTopRightUVOffset, (Vector2)_depthTexturesLoadingManager.TexTopRightUVOffset);
            _props.SetFloat(DepthTexturesUVInvScale, _depthTexturesLoadingManager.DepthTextureRcpUVScale);
            _props.SetFloat(TopDownNearPlane, _depthTexturesLoadingManager.NearPlane);
            _props.SetFloat(TopDownFarPlane, _depthTexturesLoadingManager.FarPlane);
            _props.SetFloat(TopDownYCamera, _depthTexturesLoadingManager.CameraWorldPosY);
            _props.SetFloat(MaxHeightDiff, _depthTexturesLoadingManager.MaxHeightDiff);

            _props.SetMatrix(TopDownVp, _depthTexturesLoadingManager.CameraViewProjectionMatrix);
            _props.SetMatrix(TopDownV, _depthTexturesLoadingManager.WorldToCameraMatrixFlippedZ);

            var tempNormalBufferRTHandle = (RTHandle)_tempNormalBufferHandle;
            CoreUtils.SetRenderTarget(ctx.cmd, tempNormalBufferRTHandle, ctx.cameraDepthBuffer);
            CoreUtils.DrawFullScreen(ctx.cmd, wetnessMaterial, _props);
            CustomPassUtils.Copy(ctx, tempNormalBufferRTHandle, ctx.cameraNormalBuffer);
            ExecuteMarker.End();
        }
    }
}