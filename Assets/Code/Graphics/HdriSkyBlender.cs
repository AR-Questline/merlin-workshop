using System;
using Awaken.TG.Graphics.VFX;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Graphics;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics {
    [ExecuteAlways, DefaultExecutionOrder(-100)]
    public class HdriSkyBlender : MonoBehaviour {
        static readonly int MainTex   = Shader.PropertyToID("_MainTex");
        static readonly int BlendTex  = Shader.PropertyToID("_BlendTex");
        static readonly int FaceIndex = Shader.PropertyToID("_faceIndex");
        static readonly int Blend     = Shader.PropertyToID("_blend");
        static readonly int TintId    = Shader.PropertyToID("_Tint");

        [SerializeField] Volume _volumeFrom;
        [SerializeField] Volume _volumeTo;
        [SerializeField] RenderTexture _blendedHdriCubemap;
        
        [SerializeField, LabelText("Default Tint")] Color tint = Color.white;

        [ShowInInspector, ReadOnly, NonSerialized] Color _activeTint;
        
        public Color TintWithLowerPriority(byte priority) => _tintOverride.HasValueSource ? _tintOverride.GetValueWithLowerPriority(priority) : tint;

        LightWithOverride.ValueWithOverrideWrapper<Color, HdriSkyBlender> _tintOverride;
        
        RenderTexture _originalBlendedHdriCubemap;
        Material _cubemapBlitter;
        CommandBuffer _cmd;

        float _previousBlend = float.NaN;
        Color _previousTint = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
        
        public void StartTintOverride(byte priority) {
            _tintOverride.StartOverride(priority);
        }
        public void SetTintOverride(Color tint, byte priority) {
            _tintOverride.SetOverrideValue(tint, priority);
        }
        
        public void StopTintOverride(byte priority) {
            _tintOverride.StopOverride(priority);
        }
        
        unsafe void Awake() {
            _activeTint = tint;
            _tintOverride = new(this, &GetTint, &SetTint);
        }

        void OnDestroy() {
            _tintOverride.Dispose();
        }

        void OnEnable() {
            if (!_tintOverride.HasValueSource) {
                Awake();
            }
            _cubemapBlitter = new Material(Shader.Find("Hidden/BlendCubemap"));
            _cmd = new CommandBuffer { name = "HdriSkyBlender" };

            // Nie modyfikuj assetu RT podczas PlayMode
            if (Application.isEditor && Application.isPlaying && _blendedHdriCubemap) {
                _originalBlendedHdriCubemap = _blendedHdriCubemap;
                _blendedHdriCubemap = Instantiate(_blendedHdriCubemap);
                var blendedVolume = GetComponent<Volume>();
                if (blendedVolume &&
                    blendedVolume.GetSharedOrInstancedProfile() &&
                    blendedVolume.GetSharedOrInstancedProfile().TryGet<HDRISky>(out var sky)) {
                    sky.hdriSky.Override(_blendedHdriCubemap);
                }
            }
            
            _previousBlend = float.NaN;
            _previousTint  = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
        }

        void OnDisable() {
            if (_cubemapBlitter) {
                GameObjects.DestroySafely(_cubemapBlitter);
                _cubemapBlitter = null;
            }
            if (_cmd != null) {
                _cmd.Clear();
                _cmd.Dispose();
                _cmd = null;
            }
            
            if (Application.isEditor && Application.isPlaying) {
                if (_blendedHdriCubemap && _originalBlendedHdriCubemap && _blendedHdriCubemap != _originalBlendedHdriCubemap) {
                    GameObjects.DestroySafely(_blendedHdriCubemap);
                    _blendedHdriCubemap = _originalBlendedHdriCubemap;
                    _originalBlendedHdriCubemap = null;
                    var blendedVolume = GetComponent<Volume>();
                    if (blendedVolume &&
                        blendedVolume.GetSharedOrInstancedProfile() &&
                        blendedVolume.GetSharedOrInstancedProfile().TryGet<HDRISky>(out var sky)) {
                        sky.hdriSky.Override(_blendedHdriCubemap);
                    }
                }
            }
        }
        
        void OnValidate() {
            _previousBlend = float.NaN;
            _previousTint  = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
            if (_tintOverride is {HasValueSource: true}) {
                _tintOverride.Value = tint;
            } else {
                _activeTint = tint;
            }
        }

        void Update() {
            var hasVolumeFrom = _volumeFrom && _volumeFrom.GetSharedOrInstancedProfile();
            var hasVolumeTo   = _volumeTo   && _volumeTo.GetSharedOrInstancedProfile();
            if (!hasVolumeFrom || !hasVolumeTo || !_blendedHdriCubemap || _cmd == null || !_cubemapBlitter) {
                return;
            }
            
            if (!_volumeFrom.GetSharedOrInstancedProfile().TryGet<HDRISky>(out var skyA) || skyA.hdriSky.value == null) {
                return;
            }
            if (!_volumeTo.GetSharedOrInstancedProfile().TryGet<HDRISky>(out var skyB) || skyB.hdriSky.value == null) {
                return;
            }

            float blend = 1f - _volumeFrom.weight;

            
            bool blendChanged = !Approximately(blend, _previousBlend, 0.001f);
            Color linearTint = _activeTint.linear;
            bool tintChanged = !Approximately(linearTint, _previousTint, 1f / 1024f);
            
            if (!blendChanged && !tintChanged) {
                return;
            }

            _previousBlend = blend;
            _previousTint  = linearTint;

            _cmd.Clear();

            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture(MainTex,  skyA.hdriSky.value);
            propertyBlock.SetTexture(BlendTex, skyB.hdriSky.value);
            propertyBlock.SetColor(TintId, linearTint);

            for (int i = 0; i < 6; ++i) {
                CoreUtils.SetRenderTarget(_cmd, _blendedHdriCubemap, ClearFlag.None, 0, (CubemapFace)i);
                propertyBlock.SetFloat(FaceIndex, i);
                propertyBlock.SetFloat(Blend, blend);
                _cmd.DrawProcedural(Matrix4x4.identity, _cubemapBlitter, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
            }

            UnityEngine.Graphics.ExecuteCommandBuffer(_cmd);
            var pipeline = (HDRenderPipeline) RenderPipelineManager.currentPipeline;
            pipeline?.RequestSkyEnvironmentUpdate();
        }

        static bool Approximately(float a, float b, float eps) {
            return Mathf.Abs(a - b) <= eps;
        }

        static bool Approximately(Color a, Color b, float eps) {
            return Mathf.Abs(a.r - b.r) <= eps
                && Mathf.Abs(a.g - b.g) <= eps
                && Mathf.Abs(a.b - b.b) <= eps
                && Mathf.Abs(a.a - b.a) <= eps;
        }

        static Color GetTint(HdriSkyBlender me) {
            return me._activeTint;
        }
        
        static void SetTint(HdriSkyBlender me, Color tint) {
            me._activeTint = tint;
        }
    }
}
