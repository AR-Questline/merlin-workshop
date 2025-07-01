using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.VFX {
    [Serializable, VolumeComponentMenu("Post-processing/Custom/DirectionalBlur")]
    public sealed class DirectionalBlur : CustomPostProcessVolumeComponent, IPostProcessComponent {
        [Tooltip("Controls the intensity of the effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        
        [Tooltip("Controls the width of the unblurred area.")]
        public ClampedFloatParameter unblurredArea = new ClampedFloatParameter(0.2f, 0f, 1f);
        
        [Tooltip("Controls the width of the mask.")]
        public ClampedFloatParameter blendWidth = new ClampedFloatParameter(0.1f, 0f, 1f);
        
        [Tooltip("Number of samples for the blur effect.")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(64, 2, 128);
        
        [Tooltip("Center of the blur effect.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));
        
        [Tooltip("Controls the dampening of the intensity based on the distance from the center of the screen.")]
        public ClampedFloatParameter offsetIntensityDampening = new ClampedFloatParameter(2f, 0f, 3f);

        [Tooltip("Debug view for mask.")]
        public BoolParameter debugMask = new BoolParameter(false);

        Material _material;
        
        int _intensityID;
        int _unblurredAreaID;
        int _blendWidthID;
        int _sampleCountID;
        int _centerID;
        int _debugMaskID;
        int _mainTexID;

        float CenterOffset => (center.value - new Vector2(0.5f, 0.5f)).magnitude;
        float Intensity => intensity.value / (1.0f + CenterOffset * offsetIntensityDampening.value);
        
        public bool IsActive() => _material != null && intensity.value > 0f;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup() {
            if (Shader.Find("Hidden/Shader/DirectionalBlur") != null) {
                _material = new Material(Shader.Find("Hidden/Shader/DirectionalBlur"));
                
                _intensityID = Shader.PropertyToID("_Intensity");
                _unblurredAreaID = Shader.PropertyToID("_UnblurredArea");
                _blendWidthID = Shader.PropertyToID("_BlendWidth");
                _sampleCountID = Shader.PropertyToID("_SampleCount");
                _centerID = Shader.PropertyToID("_Center");
                _debugMaskID = Shader.PropertyToID("_DebugMask");
                _mainTexID = Shader.PropertyToID("_MainTex");
            }
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination) {
            if (_material == null)
                return;

            _material.SetFloat(_intensityID, Intensity);
            _material.SetFloat(_unblurredAreaID, unblurredArea.value);
            _material.SetFloat(_blendWidthID, blendWidth.value);
            _material.SetInt(_sampleCountID, sampleCount.value);
            _material.SetVector(_centerID, center.value);
            _material.SetFloat(_debugMaskID, debugMask.value ? 1.0f : 0.0f);
            _material.SetTexture(_mainTexID, source);

            HDUtils.DrawFullScreen(cmd, _material, destination);
        }

        public override void Cleanup() => CoreUtils.Destroy(_material);
    }
}
