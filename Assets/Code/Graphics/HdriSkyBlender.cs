using Awaken.Utility.GameObjects;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics {
    [ExecuteAlways]
    public class HdriSkyBlender : MonoBehaviour {
        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        static readonly int BlendTex = Shader.PropertyToID("_BlendTex");
        static readonly int FaceIndex = Shader.PropertyToID("_faceIndex");
        static readonly int Blend = Shader.PropertyToID("_blend");
        
        [SerializeField] Volume _volumeFrom;
        [SerializeField] Volume _volumeTo;
        [SerializeField] RenderTexture _blendedHdriCubemap;
        
        RenderTexture _originalBlendedHdriCubemap;

        Material _cubemapBlitter;
        CommandBuffer _cmd;
        float _previousBlend;

        void OnEnable() {
            _cubemapBlitter = new(Shader.Find("Hidden/BlendCubemap"));
            _cmd = new();
            
            // In editor/git we don't want to have changes from this RT
            if (Application.isEditor && Application.isPlaying) {
                _originalBlendedHdriCubemap = _blendedHdriCubemap;
                _blendedHdriCubemap = Instantiate(_blendedHdriCubemap);
                var blendedVolume = GetComponent<Volume>();
                if (blendedVolume &&
                    blendedVolume.GetSharedOrInstancedProfile() &&
                    blendedVolume.GetSharedOrInstancedProfile().TryGet<HDRISky>(out var sky)) {
                    sky.hdriSky.Override(_blendedHdriCubemap);
                }
            }
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
            
            // In editor/git we don't want to have changes from this RT
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

        void Update() {
            var hasVolumeFrom = _volumeFrom && _volumeFrom.GetSharedOrInstancedProfile();
            var hasVolumeTo = _volumeTo && _volumeTo.GetSharedOrInstancedProfile();
            if (!hasVolumeFrom || !hasVolumeTo || !_blendedHdriCubemap || _cmd == null || !_cubemapBlitter) {
                return;
            }

            if (!_volumeFrom.GetSharedOrInstancedProfile().TryGet<HDRISky>(out var skyA) && skyA.hdriSky.value) {
                return;
            }
            if (!_volumeTo.GetSharedOrInstancedProfile().TryGet<HDRISky>(out var skyB) && skyA.hdriSky.value) {
                return;
            }

            var blend = 1 - _volumeFrom.weight;
            var blendChanged = Mathf.Abs(blend-_previousBlend) > 0.001f;
            if (!blendChanged) {
                return;
            }
            _previousBlend = blend;
            
            _cmd.Clear();
            
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture(MainTex, skyA.hdriSky.value);
            propertyBlock.SetTexture(BlendTex, skyB.hdriSky.value);
            
            for (int i = 0; i < 6; ++i) {
                CoreUtils.SetRenderTarget(_cmd, _blendedHdriCubemap, ClearFlag.None, 0, (CubemapFace)i);
                propertyBlock.SetFloat(FaceIndex, i);
                propertyBlock.SetFloat(Blend, blend);
                _cmd.DrawProcedural(Matrix4x4.identity, _cubemapBlitter, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
            }

            UnityEngine.Graphics.ExecuteCommandBuffer(_cmd);
        }
    }
}
