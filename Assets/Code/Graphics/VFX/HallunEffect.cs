using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.VFX {
    public enum PrismType { Horizontal = 0, Radial = 1 }

    [Serializable, VolumeComponentMenu("Post-processing/Custom/HallunEffect")]
    public sealed class HallunEffect : CustomPostProcessVolumeComponent, IPostProcessComponent {
        // ─────────────────────────────────────────────────────────────────────────────
        // Spectral Fringing
        [Header("Spectral Fringing")]
        public ClampedFloatParameter spectralIntensity      = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter spectralFalloff        = new ClampedFloatParameter(5f, 0f, 16f);
        public ClampedFloatParameter spectralBlur           = new ClampedFloatParameter(2f, 0f, 64f);
        public ClampedFloatParameter spectralStepMultiplier = new ClampedFloatParameter(2f, 0f, 10f);
        public ClampedIntParameter   spectralSampleCount    = new ClampedIntParameter(25, 2, 128);
        public Vector2Parameter      spectralCenter         = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        // ─────────────────────────────────────────────────────────────────────────────
        // Hallucination Drift
        [Space]
        [Header("Hallucination Drift")]
        public ClampedFloatParameter halluIntensity         = new ClampedFloatParameter(0.35f, 0f, 1f);
        public ClampedFloatParameter halluStrength          = new ClampedFloatParameter(0.003f, 0f, 0.01f);
        public ClampedFloatParameter halluSpeed             = new ClampedFloatParameter(0.6f, 0f, 5f);
        public ClampedFloatParameter halluNoiseTiling       = new ClampedFloatParameter(8f, 0.5f, 32f);
        public ClampedFloatParameter halluNoiseSpeed        = new ClampedFloatParameter(0.5f, 0f, 5f);
        public ClampedFloatParameter halluPulseSpeed        = new ClampedFloatParameter(2f, 0f, 10f);
        public ClampedFloatParameter halluPulseMin          = new ClampedFloatParameter(1f, 0f, 4f);
        public ClampedFloatParameter halluPulseMax          = new ClampedFloatParameter(2f, 0f, 4f);
        public ClampedFloatParameter halluEdgeBoost         = new ClampedFloatParameter(0f, 0f, 2f);

        // ─────────────────────────────────────────────────────────────────────────────
        // Fisheye Distortion
        [Space]
        [Header("Fisheye Distortion")]
        public ClampedFloatParameter fishIntensity          = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter fishAmount             = new ClampedFloatParameter(0.35f, -1f, 1f);
        public ClampedFloatParameter fishPower              = new ClampedFloatParameter(1.0f, 0f, 3f);

        // ─────────────────────────────────────────────────────────────────────────────
        // Prism Replicator
        [Space]
        [Header("Prism Replicator")]
        public ClampedFloatParameter prismIntensity         = new ClampedFloatParameter(1f, 0f, 1f);
        public EnumParameter<PrismType> prismType           = new EnumParameter<PrismType>(PrismType.Radial);

        [Space, Header("Common")]
        public ClampedIntParameter   prismCopies            = new ClampedIntParameter(7, 2, 16);
        public ClampedFloatParameter prismScale             = new ClampedFloatParameter(0.95f, 0.5f, 1.2f);
        public ClampedFloatParameter prismFeather           = new ClampedFloatParameter(0.8f, 0.1f, 2f);
        public ClampedFloatParameter prismFalloff           = new ClampedFloatParameter(1.2f, 0.2f, 4f);
        public ClampedFloatParameter prismJitter            = new ClampedFloatParameter(0.15f, 0f, 1f);
        public ClampedFloatParameter prismJitterSpeed       = new ClampedFloatParameter(0.5f, 0f, 5f);
        public ClampedFloatParameter prismReseedFadeSeconds = new ClampedFloatParameter(0.35f, 0f, 2f);
        public ClampedFloatParameter phaseDrift             = new ClampedFloatParameter(0.05f, 0f, 0.2f);

        [Space, Header("Horizontal only")]
        public ClampedFloatParameter prismStep              = new ClampedFloatParameter(0.035f, 0.001f, 0.25f);
        public ClampedFloatParameter prismAxisDeg           = new ClampedFloatParameter(0f, -180f, 180f);

        [Space, Header("Radial only")]
        public ClampedFloatParameter prismRadius            = new ClampedFloatParameter(0.14f, 0f, 0.6f);
        public ClampedIntParameter   prismSeed              = new ClampedIntParameter(0, 0, 9999);
        public BoolParameter         prismReseedOnPlay      = new BoolParameter(false);

        // ─────────────────────────────────────────────────────────────────────────────
        // RGB Flow
        [Space]
        [Header("RGB Flow")]
        public ClampedFloatParameter rgbFlowIntensity       = new ClampedFloatParameter(0.35f, 0f, 1f);
        public ClampedFloatParameter rgbFlowTiling          = new ClampedFloatParameter(6f, 0.25f, 16f);
        public ClampedFloatParameter rgbFlowSpeed           = new ClampedFloatParameter(0.8f, 0f, 5f);
        public ClampedFloatParameter rgbFlowContrast        = new ClampedFloatParameter(1.2f, 0.8f, 1.6f);

        // ─────────────────────────────────────────────────────────────────────────────
        // Border Mask
        [Space]
        [Header("Border Mask")]
        public BoolParameter    maskEnable                  = new BoolParameter(false);
        public Vector2Parameter maskCenter                  = new Vector2Parameter(new Vector2(0.5f, 0.5f));
        public ClampedFloatParameter maskRadius             = new ClampedFloatParameter(0.55f, 0.1f, 1.5f);
        public ClampedFloatParameter maskSoftness           = new ClampedFloatParameter(4f, 0f, 8f);
        public BoolParameter    maskInvert                  = new BoolParameter(true);

        // ---------- Runtime ----------
        Material mat;
        int   _runtimePrismSeed;
        float _prismSessionValue;
        bool  _prevPrismActive;
        float _reseedStartTime;
        int   _oldPrismSeed;

        // IDs
        int _MainTex;

        // Spectral
        int _SpectralIntensity, _SpectralFalloff, _SpectralBlur, _SpectralStepMultiplier, _SpectralSampleCount, _SpectralCenter;

        // Hallu
        int _HalluIntensity, _HalluStrength, _HalluSpeed, _HalluNoiseTiling, _HalluNoiseSpeed, _HalluPulseSpeed, _HalluPulseMin, _HalluPulseMax, _HalluEdgeBoost;

        // Fish
        int _FishIntensity, _FishAmount, _FishPower;

        // Prism
        int _PrismIntensity, _PrismCopies, _PrismType, _PrismStep, _PrismAxisDeg, _PrismScale, _PrismFeather, _PrismFalloff, _PrismJitter, _PrismJitterSpeed, _PrismRadius, _PrismSeed;
        int _PrismSession, _PrismOldSeed, _PrismReseedStartTime, _PrismReseedFadeSeconds, _PhaseDrift;

        // Flow
        int _RGBFlowIntensity, _RGBFlowTiling, _RGBFlowSpeed, _RGBFlowContrast;

        // Mask
        int _MaskEnable, _MaskCenter, _MaskRadius, _MaskSoftness, _MaskInvert;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public bool IsActive() {
            if (mat == null || !active) return false;
            
            bool spectral = spectralIntensity.value  > 0f && spectralBlur.value > 0f && spectralSampleCount.value > 1;
            bool hallu    = halluIntensity.value     > 0f && halluStrength.value > 0f;
            bool fish     = fishIntensity.value      > 0f && Mathf.Abs(fishAmount.value) > 0f;
            bool prism    = prismIntensity.value     > 0f && prismCopies.value >= 2;
            bool flow     = rgbFlowIntensity.value   > 0f;
            
            return spectral || hallu || fish || prism || flow;
        }

        // Required by HDRP (paired with IsActive)
        public bool IsTileCompatible() => false;

        public override void Setup() {
            var shader = Shader.Find("Hidden/Shader/HallunEffect");
            if (shader == null) return;
            
            mat = new Material(shader);

            _MainTex = Shader.PropertyToID("_MainTex");

            _SpectralIntensity      = Shader.PropertyToID("_SpectralIntensity");
            _SpectralFalloff        = Shader.PropertyToID("_SpectralFalloff");
            _SpectralBlur           = Shader.PropertyToID("_SpectralBlur");
            _SpectralStepMultiplier = Shader.PropertyToID("_SpectralStepMultiplier");
            _SpectralSampleCount    = Shader.PropertyToID("_SpectralSampleCount");
            _SpectralCenter         = Shader.PropertyToID("_SpectralCenter");

            _HalluIntensity         = Shader.PropertyToID("_HalluIntensity");
            _HalluStrength          = Shader.PropertyToID("_HalluStrength");
            _HalluSpeed             = Shader.PropertyToID("_HalluSpeed");
            _HalluNoiseTiling       = Shader.PropertyToID("_HalluNoiseTiling");
            _HalluNoiseSpeed        = Shader.PropertyToID("_HalluNoiseSpeed");
            _HalluPulseSpeed        = Shader.PropertyToID("_HalluPulseSpeed");
            _HalluPulseMin          = Shader.PropertyToID("_HalluPulseMin");
            _HalluPulseMax          = Shader.PropertyToID("_HalluPulseMax");
            _HalluEdgeBoost         = Shader.PropertyToID("_HalluEdgeBoost");

            _FishIntensity          = Shader.PropertyToID("_FishIntensity");
            _FishAmount             = Shader.PropertyToID("_FishAmount");
            _FishPower              = Shader.PropertyToID("_FishPower");

            _PrismIntensity         = Shader.PropertyToID("_PrismIntensity");
            _PrismCopies            = Shader.PropertyToID("_PrismCopies");
            _PrismType              = Shader.PropertyToID("_PrismType");
            _PrismStep              = Shader.PropertyToID("_PrismStep");
            _PrismAxisDeg           = Shader.PropertyToID("_PrismAxisDeg");
            _PrismScale             = Shader.PropertyToID("_PrismScale");
            _PrismFeather           = Shader.PropertyToID("_PrismFeather");
            _PrismFalloff           = Shader.PropertyToID("_PrismFalloff");
            _PrismJitter            = Shader.PropertyToID("_PrismJitter");
            _PrismJitterSpeed       = Shader.PropertyToID("_PrismJitterSpeed");
            _PrismRadius            = Shader.PropertyToID("_PrismRadius");
            _PrismSeed              = Shader.PropertyToID("_PrismSeed");
            _PrismSession           = Shader.PropertyToID("_PrismSession");
            _PrismOldSeed           = Shader.PropertyToID("_PrismOldSeed");
            _PrismReseedStartTime   = Shader.PropertyToID("_PrismReseedStartTime");
            _PrismReseedFadeSeconds = Shader.PropertyToID("_PrismReseedFadeSeconds");
            _PhaseDrift             = Shader.PropertyToID("_PhaseDrift");

            _RGBFlowIntensity       = Shader.PropertyToID("_RGBFlowIntensity");
            _RGBFlowTiling          = Shader.PropertyToID("_RGBFlowTiling");
            _RGBFlowSpeed           = Shader.PropertyToID("_RGBFlowSpeed");
            _RGBFlowContrast        = Shader.PropertyToID("_RGBFlowContrast");

            _MaskEnable             = Shader.PropertyToID("_MaskEnable");
            _MaskCenter             = Shader.PropertyToID("_MaskCenter");
            _MaskRadius             = Shader.PropertyToID("_MaskRadius");
            _MaskSoftness           = Shader.PropertyToID("_MaskSoftness");
            _MaskInvert             = Shader.PropertyToID("_MaskInvert");

            _runtimePrismSeed = prismReseedOnPlay.value ? UnityEngine.Random.Range(0, 1000000) : prismSeed.value;
            _prismSessionValue = UnityEngine.Random.Range(0f, 10000f);
            _prevPrismActive = false;
            _oldPrismSeed = _runtimePrismSeed;
            _reseedStartTime = -999f;
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination) {
            if (mat == null || !IsActive()) {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            bool prismActive = prismIntensity.value > 0f && prismCopies.value >= 2;
            if (prismActive && !_prevPrismActive) {
                _oldPrismSeed      = _runtimePrismSeed;
                _runtimePrismSeed  = prismSeed.value;
                _prismSessionValue = UnityEngine.Random.Range(0f, 10000f);
                _reseedStartTime   = Time.time;
            }
            _prevPrismActive = prismActive;

            mat.SetTexture(_MainTex, source);

            // Spectral
            mat.SetFloat (_SpectralIntensity, spectralIntensity.value);
            mat.SetFloat (_SpectralFalloff, spectralFalloff.value);
            mat.SetFloat (_SpectralBlur, spectralBlur.value);
            mat.SetFloat (_SpectralStepMultiplier, spectralStepMultiplier.value);
            mat.SetInt   (_SpectralSampleCount, spectralSampleCount.value);
            mat.SetVector(_SpectralCenter, spectralCenter.value);

            // Hallu
            mat.SetFloat(_HalluIntensity, halluIntensity.value);
            mat.SetFloat(_HalluStrength, halluStrength.value);
            mat.SetFloat(_HalluSpeed, halluSpeed.value);
            mat.SetFloat(_HalluNoiseTiling, halluNoiseTiling.value);
            mat.SetFloat(_HalluNoiseSpeed, halluNoiseSpeed.value);
            mat.SetFloat(_HalluPulseSpeed, halluPulseSpeed.value);
            mat.SetFloat(_HalluPulseMin, halluPulseMin.value);
            mat.SetFloat(_HalluPulseMax, halluPulseMax.value);
            mat.SetFloat(_HalluEdgeBoost, halluEdgeBoost.value);

            // Fish
            mat.SetFloat(_FishIntensity, fishIntensity.value);
            mat.SetFloat(_FishAmount, fishAmount.value);
            mat.SetFloat(_FishPower, fishPower.value);

            // Prism
            mat.SetFloat(_PrismIntensity, prismIntensity.value);
            mat.SetInt  (_PrismCopies, prismCopies.value);
            mat.SetFloat(_PrismType, (int)prismType.value);
            mat.SetFloat(_PrismStep, prismStep.value);
            mat.SetFloat(_PrismAxisDeg, prismAxisDeg.value);
            mat.SetFloat(_PrismScale, prismScale.value);
            mat.SetFloat(_PrismFeather, prismFeather.value);
            mat.SetFloat(_PrismFalloff, prismFalloff.value);
            mat.SetFloat(_PrismJitter, prismJitter.value);
            mat.SetFloat(_PrismJitterSpeed, prismJitterSpeed.value);
            mat.SetFloat(_PrismRadius, prismRadius.value);
            mat.SetInt  (_PrismSeed, _runtimePrismSeed);
            mat.SetFloat(_PrismSession, _prismSessionValue);
            mat.SetInt  (_PrismOldSeed, _oldPrismSeed);
            mat.SetFloat(_PrismReseedStartTime, _reseedStartTime);
            mat.SetFloat(_PrismReseedFadeSeconds, prismReseedFadeSeconds.value);
            mat.SetFloat(_PhaseDrift, phaseDrift.value);

            // Flow
            mat.SetFloat(_RGBFlowIntensity, rgbFlowIntensity.value);
            mat.SetFloat(_RGBFlowTiling, rgbFlowTiling.value);
            mat.SetFloat(_RGBFlowSpeed, rgbFlowSpeed.value);
            mat.SetFloat(_RGBFlowContrast, rgbFlowContrast.value);

            // Mask
            mat.SetFloat(_MaskEnable, maskEnable.value ? 1f : 0f);
            mat.SetVector(_MaskCenter,maskCenter.value);
            mat.SetFloat(_MaskRadius, maskRadius.value);
            mat.SetFloat(_MaskSoftness, maskSoftness.value);
            mat.SetFloat(_MaskInvert, maskInvert.value ? 1f : 0f);

            HDUtils.DrawFullScreen(cmd, mat, destination);
        }

        public override void Cleanup() => CoreUtils.Destroy(mat);
    }
}
