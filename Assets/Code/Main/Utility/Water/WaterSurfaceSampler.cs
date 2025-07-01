using System;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Utility.Water {
    public class WaterSurfaceSampler : IDisposable {
        const int WaterPatchesBandCount = 3;
        const string WaterSampleKernelName = "CSSample";
        static readonly int SampleResultsId = Shader.PropertyToID("_Results");
        static readonly int SampleInputUvId = Shader.PropertyToID("_UVs");
        static readonly int SampleInputTextureId = Shader.PropertyToID("_WaterSurface");

        readonly Settings _settings;
        readonly ComputeBuffer _waterSampleInputBuffer;
        readonly ComputeBuffer _waterSampleResultsBuffer;
        readonly int _waterSampleKernel;
        
        NativeArray<float2> _waterSampleInputs;
        NativeArray<Color> _waterSampleResults;
        WaterSurface _sampledWaterSurface;
        Transform _sampledWaterSurfaceTransform;
        AsyncGPUReadbackRequest? _lastRequest;
        
        Vector2 _correctionOffset;
        bool _canSampleWaterSurface;
        bool _isSampling;
        
        public Vector3 RawOffset { get; private set; }
        public Vector3 EasedOffset { get; private set; }
        
        public WaterSurfaceSampler(Settings settings) {
            _settings = settings;
            
            _waterSampleInputs = new(WaterPatchesBandCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _waterSampleResults = new(WaterPatchesBandCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _waterSampleKernel = _settings.shader.FindKernel(WaterSampleKernelName);
            _waterSampleInputBuffer = new(WaterPatchesBandCount, UnsafeUtility.SizeOf<float2>(), ComputeBufferType.Structured);
            _waterSampleResultsBuffer = new(WaterPatchesBandCount, UnsafeUtility.SizeOf<Color>(), ComputeBufferType.Structured);
        }

        public void RequestSample(WaterSurface waterSurface, Vector3 samplePosition) {
            if (_isSampling) {
                return;
            }
            
            if (_sampledWaterSurface != waterSurface) {
                RawOffset = Vector3.zero;
                EasedOffset = Vector3.zero;
                _sampledWaterSurface = waterSurface;
                if (waterSurface != null) {
                    _sampledWaterSurfaceTransform = waterSurface.transform;
                    // _canSampleWaterSurface = waterSurface.IsSimulationActive() && waterSurface.allowSimplifiedSampling;
                } else {
                    _sampledWaterSurfaceTransform = null;
                    _canSampleWaterSurface = false;
                }
            }
            
            if (!_canSampleWaterSurface) {
                return;
            }

            // var displacementBuffer = _sampledWaterSurface.GetDisplacementBuffer();
            // if (displacementBuffer == null) {
            //     return;
            // }

            // var correctedSamplePos = samplePosition - new Vector3(_correctionOffset.x, 0, _correctionOffset.y);
            // _waterSampleInputs[0] = _sampledWaterSurface.GetSimplifiedPatchCoordsAtWorldPoint(correctedSamplePos, 0);
            // _waterSampleInputs[1] = _sampledWaterSurface.GetSimplifiedPatchCoordsAtWorldPoint(correctedSamplePos, 1);
            // _waterSampleInputs[2] = _sampledWaterSurface.GetSimplifiedPatchCoordsAtWorldPoint(correctedSamplePos, 2);
            // _waterSampleInputBuffer.SetData(_waterSampleInputs);
            //
            // _settings.shader.SetBuffer(_waterSampleKernel, SampleInputUvId, _waterSampleInputBuffer);
            // _settings.shader.SetBuffer(_waterSampleKernel, SampleResultsId, _waterSampleResultsBuffer);
            // _settings.shader.SetTexture(_waterSampleKernel, SampleInputTextureId, displacementBuffer);
            // _settings.shader.Dispatch(_waterSampleKernel, WaterPatchesBandCount, 1, 1);
            //
            // _isSampling = true;
            // StartBufferReadback().Forget();
        }

        async UniTaskVoid StartBufferReadback() {
            var request = AsyncGPUReadback.RequestIntoNativeArray(ref _waterSampleResults, _waterSampleResultsBuffer);
            _lastRequest = request;
            
            while (!request.done) {
                await UniTask.NextFrame(PlayerLoopTiming.PostLateUpdate);
            }
            
            OnBufferReadbackCompleted();
        }

        void OnBufferReadbackCompleted() {
            if (_lastRequest is { hasError: false } && _waterSampleResults.IsCreated && _sampledWaterSurface != null) {
                // var localOffset = 
                //     _sampledWaterSurface.ConvertPatchSampleIntoLocalOffset(_waterSampleResults[0], 0) +
                //     _sampledWaterSurface.ConvertPatchSampleIntoLocalOffset(_waterSampleResults[1], 1) +
                //     _sampledWaterSurface.ConvertPatchSampleIntoLocalOffset(_waterSampleResults[2], 2);
                //
                // var patchAmplitudeSum = _sampledWaterSurface.GetPatchAmplitudeMultiplier(0) + 
                //                         _sampledWaterSurface.GetPatchAmplitudeMultiplier(1) +
                //                         _sampledWaterSurface.GetPatchAmplitudeMultiplier(2);
                //
                // localOffset.y += patchAmplitudeSum * _settings.waveBasedVerticalOffset;
                //
                // RawOffset = _sampledWaterSurfaceTransform.TransformVector(localOffset);
                // _correctionOffset = Vector2.Lerp(_correctionOffset, new Vector2(RawOffset.x, RawOffset.z), _settings.correctionFactor);
            }
            _isSampling = false;
        }

        public void ProgressEasing(float deltaTime) {
            EasedOffset = Vector3.Lerp(EasedOffset, RawOffset, _settings.easingForce * deltaTime);
        }
        
        public void Dispose() {
            _isSampling = false;
            
            if (_lastRequest != null) {
                _lastRequest.Value.WaitForCompletion();
                _lastRequest = null;
            }
            
            _waterSampleInputBuffer?.Dispose();
            _waterSampleInputs.Dispose();
            _waterSampleResultsBuffer?.Dispose();
            _waterSampleResults.Dispose();
            _sampledWaterSurface = null;
            _sampledWaterSurfaceTransform = null;
        }

        [Serializable]
        public struct Settings {
            public ComputeShader shader;
            public float correctionFactor;
            public float easingForce;
            public float waveBasedVerticalOffset;
        }
    }
}