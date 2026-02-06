using System;
using System.Runtime.InteropServices;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Sirenix.OdinInspector;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public abstract class VoiceOversEventEmitter : StudioEventEmitter {
        [FoldoutGroup("Spectrum Settings")] [SerializeField] float averageModifier = 500f;
        [FoldoutGroup("Spectrum Settings")] [Tooltip("how many samples of audio; must be the power of 2")] [SerializeField] uint windowSize = 32;
        [FoldoutGroup("Spectrum Settings")] [SerializeField] DSP_FFT_WINDOW_TYPE windowShape = DSP_FFT_WINDOW_TYPE.HAMMING; //fft - Fast Fourier Transform
        [SerializeField] bool timeScaleDependent = true;
        
        Transform _headTransform;
        // ChannelGroup _channelGroup;
        // DSP _dsp; //digital signal processor

        protected bool ARAllowFadeout => false;
        
        // === Initialization
        protected override void Awake() {
            base.Awake();
            // RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out _dsp);
            // _dsp.setParameterInt((int) DSP_FFT.WINDOW, (int)windowShape);
            // _dsp.setParameterInt((int) DSP_FFT.WINDOWSIZE, (int)(windowSize * 2));
            Prepare();
        }
        
        public void SetHeadTransform(Transform headTransform) {
            // SetEmitterPositionTransform(headTransform);
        }
        
        // === Public API
        public async UniTaskVoid Speak(EventReference newEvent, EmotionData[] emotions) {
            if (newEvent.IsNull) {
                // Stop();
                return;
            }
            
            // ChangeEvent(newEvent);
            // Play();

            if (timeScaleDependent) {
                World.Services.Get<UnityUpdateProvider>().RegisterStudioEventEmitter(this);
            }

            OnSpeakingStarted(emotions);
            double timePlaying = 0;
            // while (IsPlaying()) {
            //     SpeakingUpdate(timePlaying);
            //     await UniTask.DelayFrame(1);
            //     timePlaying += Time.deltaTime;
            // }
            OnSpeakingEnded();
        }

        // === Speaking Cycle
        protected virtual void Prepare() { }
        protected virtual void OnSpeakingStarted(EmotionData[] emotions) { }
        protected virtual void SpeakingUpdate(double timePlaying) { }
        protected virtual void OnSpeakingEnded() { }
        
        // === Getting voice spectrum
        protected float GetExternalAnalysis() {
            // if (!IsPlaying()) {
            //     return 0;
            // }
            
            if (GetSpectrumData(out var samples)) {
                var total = 0f;
                foreach (var sample in samples) {
                    total += sample;
                }
                float value = total / samples.Length * averageModifier;
                samples.Dispose();
                return Mathf.Clamp01(value);
            }
            return 0;
        }
        
        unsafe bool GetSpectrumData(out UnsafeArray<float> samples) {
            // if (EventInstance.isValid() == false || EventInstance.getPlaybackState(out var playbackState) != RESULT.OK || playbackState != PLAYBACK_STATE.PLAYING) {
            //     samples = default;
            //     return false;
            // }
            //
            // _dsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out IntPtr data, out uint _);
            // var fftParam = (AR_DSP_PARAMETER_FFT*)data;
            //
            // EventInstance.getChannelGroup(out _channelGroup);
            // _channelGroup.addDSP(0, _dsp);
            //
            // var numChannels = fftParam->numchannels;
            // if (numChannels == 1) {
            //     samples = new UnsafeArray<float>(windowSize, ARAlloc.Temp);
            //     UnsafeUtility.MemCpy(samples.Ptr, GetSpectrumDataPointer(fftParam, 0), windowSize * sizeof(float));
            //     return true;
            // } else if (numChannels > 1) {
            //     samples = new UnsafeArray<float>(windowSize, ARAlloc.Temp);
            //     UnsafeUtility.MemCpy(samples.Ptr, GetSpectrumDataPointer(fftParam, 0), windowSize * sizeof(float));
            //     for (int c = 1; c < numChannels; c++) {
            //         var spectrumPtr = GetSpectrumDataPointer(fftParam, c);
            //         for (var s = 0u; s < windowSize; s++) {
            //             samples[s] += spectrumPtr[s];
            //         }
            //     }
            //     for (var s = 0u; s < windowSize; s++) {
            //         samples[s] /= numChannels;
            //     }
            //     return true;
            // }
            //
            samples = default;
            return false;
        }

        // Copy of DSP_PARAMETER_FFT with spectrum_internal unrolled and public
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct AR_DSP_PARAMETER_FFT {
            public int length;
            public int numchannels;

            public float* spectrum_internal_00;
            public float* spectrum_internal_01;
            public float* spectrum_internal_02;
            public float* spectrum_internal_03;
            public float* spectrum_internal_04;
            public float* spectrum_internal_05;
            public float* spectrum_internal_06;
            public float* spectrum_internal_07;
            public float* spectrum_internal_08;
            public float* spectrum_internal_09;
            public float* spectrum_internal_10;
            public float* spectrum_internal_11;
            public float* spectrum_internal_12;
            public float* spectrum_internal_13;
            public float* spectrum_internal_14;
            public float* spectrum_internal_15;
            public float* spectrum_internal_16;
            public float* spectrum_internal_17;
            public float* spectrum_internal_18;
            public float* spectrum_internal_19;
            public float* spectrum_internal_20;
            public float* spectrum_internal_21;
            public float* spectrum_internal_22;
            public float* spectrum_internal_23;
            public float* spectrum_internal_24;
            public float* spectrum_internal_25;
            public float* spectrum_internal_26;
            public float* spectrum_internal_27;
            public float* spectrum_internal_28;
            public float* spectrum_internal_29;
            public float* spectrum_internal_30;
            public float* spectrum_internal_31;
        }

        unsafe float* GetSpectrumDataPointer(AR_DSP_PARAMETER_FFT* fftData, int channel) {
            // 1 to skip length + numchannels, (int+int) == ptr size
            return ((float**)fftData)[1 + channel];
        }
    }
}