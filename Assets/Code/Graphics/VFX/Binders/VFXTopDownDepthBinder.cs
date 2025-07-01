using Unity.Profiling;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UniversalProfiling;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/Top-down depth")]
    [VFXBinder("AR/Top-down depth camera")]
    public class VFXTopDownDepthBinder : VFXBinderBase {
        static readonly UniversalProfilerMarker UpdateBindingMarker = new("VFXTopDownDepthBinder.UpdateBinding");
        
        public TopDownDepthTexturesLoadingManager topDownDepthTexturesLoadingManager;
        ExposedProperty _depthTexBottomLeftUVOffset;
        ExposedProperty _depthTexBottomRightUVOffset;
        ExposedProperty _depthTexTopLeftUVOffset;
        ExposedProperty _depthTexTopRightUVOffset;
        
        ExposedProperty _depthTexturesArray;
        ExposedProperty _depthTexturesLayers;

        ExposedProperty _depthTextureInvUVScale;

        ExposedProperty _nearPlane;
        ExposedProperty _farPlane;
        ExposedProperty _textureSize;
        ExposedProperty _worldToCamera;
        ExposedProperty _viewToClip;

        protected override void OnEnable() {
            base.OnEnable();
            UpdateSubProperties();
        }
        
        void OnValidate() {
            UpdateSubProperties();
        }

        public override bool IsValid(VisualEffect component) {
            return topDownDepthTexturesLoadingManager != null &&
                   component.HasVector2(_depthTexBottomLeftUVOffset) &&
                   component.HasVector2(_depthTexBottomRightUVOffset) &&
                   component.HasVector2(_depthTexTopLeftUVOffset) &&
                   component.HasVector2(_depthTexTopRightUVOffset) &&
                   component.HasTexture(_depthTexturesArray) &&
                   component.HasVector4(_depthTexturesLayers) &&
                   component.HasFloat(_depthTextureInvUVScale) &&
                   component.HasFloat(_nearPlane) &&
                   component.HasFloat(_farPlane) &&
                   component.HasFloat(_textureSize) &&
                   component.HasMatrix4x4(_worldToCamera) &&
                   component.HasMatrix4x4(_viewToClip);
        }

        public override void UpdateBinding(VisualEffect component) {
            if (topDownDepthTexturesLoadingManager == null || topDownDepthTexturesLoadingManager.IsInitialized == false) {
                return;
            }
            UpdateBindingMarker.Begin();
            component.SetVector2(_depthTexBottomLeftUVOffset, topDownDepthTexturesLoadingManager.TexBottomLeftUVOffset);
            component.SetVector2(_depthTexBottomRightUVOffset, topDownDepthTexturesLoadingManager.TexBottomRightUVOffset);
            component.SetVector2(_depthTexTopLeftUVOffset, topDownDepthTexturesLoadingManager.TexTopLeftUVOffset);
            component.SetVector2(_depthTexTopRightUVOffset, topDownDepthTexturesLoadingManager.TexTopRightUVOffset);
            component.SetTexture(_depthTexturesArray, topDownDepthTexturesLoadingManager.DepthTexturesArray);
            component.SetVector4(_depthTexturesLayers, topDownDepthTexturesLoadingManager.DepthTexturesLayers);
            component.SetFloat(_depthTextureInvUVScale, topDownDepthTexturesLoadingManager.DepthTextureRcpUVScale);
            component.SetFloat(_nearPlane, topDownDepthTexturesLoadingManager.NearPlane);
            component.SetFloat(_farPlane, topDownDepthTexturesLoadingManager.FarPlane);
            component.SetFloat(_textureSize, topDownDepthTexturesLoadingManager.ChunkTextureSize);
            component.SetMatrix4x4(_worldToCamera, topDownDepthTexturesLoadingManager.WorldToCameraMatrix);
            component.SetMatrix4x4(_viewToClip, topDownDepthTexturesLoadingManager.CameraViewToClipMatrix);
            UpdateBindingMarker.End();
        }

        public override string ToString() {
            return $"Top-down depth";
        }
        
        void UpdateSubProperties() {
            _depthTexBottomLeftUVOffset = "DepthTexBottomLeftUVOffset";
            _depthTexBottomRightUVOffset = "DepthTexBottomRightUVOffset";
            _depthTexTopLeftUVOffset = "DepthTexTopLeftUVOffset";
            _depthTexTopRightUVOffset = "DepthTexTopRightUVOffset";

            _depthTexturesArray = "DepthTexturesArray";
            _depthTexturesLayers = "DepthTexturesLayers";
            _depthTextureInvUVScale = "DepthTextureInvUVScale";
            
            _nearPlane = "TopDownDepth_NearPlane";
            _farPlane = "TopDownDepth_FarPlane";
            _textureSize = "TopDownDepth_TextureSize";
            _worldToCamera = "TopDownDepth_WorldToCamera";
            _viewToClip = "TopDownDepth_ViewToClip";
        }
    }
}
