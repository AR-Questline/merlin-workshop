using System;
using System.Runtime.InteropServices;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Utility.Animations;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Main.Utility.Terrain {
    public class FootstepCategoryProvider : IDisposable {
        const int PixelComponents = 4;
        const int MaxSplatmapsCount = 2;

        NativeArray<Color> _splatmapsPixel;

        readonly ComputeShader _splatmapsSampleShader;
        readonly int _splatmapSampleKernel;
        readonly ComputeBuffer _splatmapSampleBuffer;
        AsyncGPUReadbackRequest? _lastRequest;

        public FootstepCategoryProvider(ComputeShader splatmapsSampleShader) {
            _splatmapsPixel = new(MaxSplatmapsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            _splatmapsSampleShader = splatmapsSampleShader;
            _splatmapSampleKernel = _splatmapsSampleShader.FindKernel("CSSample");
            _splatmapSampleBuffer = new(MaxSplatmapsCount, Marshal.SizeOf<Color>(), ComputeBufferType.Structured);
            _splatmapsSampleShader.SetBuffer(_splatmapSampleKernel, "_Results", _splatmapSampleBuffer);
        }

        public void Dispose() {
            if (_lastRequest != null) {
                _lastRequest.Value.WaitForCompletion();
                _lastRequest = null;
            }
            _splatmapsPixel.Dispose();
            _splatmapSampleBuffer.Dispose();
        }

        public async UniTask FillFootsteps(Texture2D[] splatmaps, int[] fmodParams, Vector2 uv, float noisiness, FMODParameter[] outputFootsteps, string defaultSurfaceFmodParamName) {
            var success = await SampleSplatmaps(splatmaps, uv);
            _lastRequest = null;

            if (success && _splatmapsPixel.IsCreated) {
                FillFootsteps(fmodParams, noisiness, outputFootsteps, defaultSurfaceFmodParamName);
            }
        }

        async UniTask<bool> SampleSplatmaps(Texture2D[] splatmaps, Vector2 uv) {
            var hasSecondSplatmap = splatmaps.Length > 1;
            _splatmapsSampleShader.SetTexture(_splatmapSampleKernel, "_Splatmap0", splatmaps[0]);
            _splatmapsSampleShader.SetTexture(_splatmapSampleKernel, "_Splatmap1", hasSecondSplatmap ? splatmaps[1] : Texture2D.blackTexture);
            _splatmapsSampleShader.SetBool("_HasSecondSplatmap", hasSecondSplatmap);
            _splatmapsSampleShader.SetVector("_UV", uv);

            _splatmapsSampleShader.Dispatch(_splatmapSampleKernel, 1, 1, 1);
            var request = AsyncGPUReadback.RequestIntoNativeArray(ref _splatmapsPixel, _splatmapSampleBuffer);
            _lastRequest = request;

            while (!request.done) {
                await UniTask.NextFrame(PlayerLoopTiming.PostLateUpdate);
            }
            return !request.hasError;
        }
        
        void FillFootsteps(int[] fmodParams, float noisiness, FMODParameter[] outputFootsteps, string defaultSurfaceFmodParamName) {
            int layersCount = math.min(fmodParams.Length, MaxSplatmapsCount * PixelComponents);
            bool isAnyParamSetToNonZero = false;
            for (var layerIndex = 0; layerIndex < layersCount; layerIndex++) {
                var splatmapIndex = layerIndex / PixelComponents;
                var pixelComponentIndex = layerIndex % PixelComponents;

                var terrainTypeIndex = fmodParams[layerIndex];
                if (terrainTypeIndex != -1) {
                    var splatValue = _splatmapsPixel[splatmapIndex][pixelComponentIndex];
                    var noise = splatValue * noisiness;
                    var terrainTypeFmodParamName = SurfaceType.TerrainTypes[terrainTypeIndex].FModParameterName;
                    if (TryGetIndexOfParamWithName(outputFootsteps, terrainTypeFmodParamName, out int indexInArray) && noise > outputFootsteps[indexInArray].value) {
                        isAnyParamSetToNonZero |= noise != 0;
                        outputFootsteps[indexInArray] = new(terrainTypeFmodParamName, noise);
                    }
                }
            }

            if (isAnyParamSetToNonZero == false) {
                if (TryGetIndexOfParamWithName(outputFootsteps, defaultSurfaceFmodParamName, out int indexInArray)) {
                    outputFootsteps[indexInArray] = new(defaultSurfaceFmodParamName, 1);
                }
            }

            static bool TryGetIndexOfParamWithName(FMODParameter[] fmodParams, string name, out int index) {
                for (int i = 0; i < fmodParams.Length; i++) {
                    if (fmodParams[i].name == name) {
                        index = i;
                        return true;
                    }
                }
                index = -1;
                return false;
            }
        }
    }
}