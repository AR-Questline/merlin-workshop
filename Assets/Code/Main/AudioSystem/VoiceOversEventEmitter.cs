using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public abstract class VoiceOversEventEmitter : StudioEventEmitter {
        [FoldoutGroup("Spectrum Settings")] [SerializeField] float averageModifier = 1000f;
        [FoldoutGroup("Spectrum Settings")] [Tooltip("how many samples of audio; must be the power of 2")] [SerializeField] int windowSize = 512;
        [FoldoutGroup("Spectrum Settings")] [SerializeField] DSP_FFT_WINDOW_TYPE windowShape = DSP_FFT_WINDOW_TYPE.HAMMING; //fft - Fast Fourier Transform
        [SerializeField] bool timeScaleDependent = true;
        
        Transform _headTransform;
        // ChannelGroup _channelGroup;
        // DSP _dsp; //digital signal processor

        float[] Samples { get; set; }
        
        // === Initialization
        protected override void Awake() {
            base.Awake();
            Samples = new float[windowSize];
            // RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out _dsp);
            // _dsp.setParameterInt((int) DSP_FFT.WINDOW, (int) windowShape);
            // _dsp.setParameterInt((int) DSP_FFT.WINDOWSIZE, windowSize * 2);
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
            
            if (GetSpectrumData()) {
                float value = Samples.Average() * averageModifier;
                return Mathf.Clamp01(value);
            }
            return 0;
        }
        
        bool GetSpectrumData() {
            // if (EventInstance.isValid() == false || EventInstance.getPlaybackState(out var playbackState) != RESULT.OK || playbackState != PLAYBACK_STATE.PLAYING) {
            //     return false;
            // }
            // _dsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out IntPtr data, out uint _);
            // var fftParam = Marshal.PtrToStructure<DSP_PARAMETER_FFT>(data);
            //
            // EventInstance.getChannelGroup(out _channelGroup);
            // _channelGroup.addDSP(0, _dsp);
            //
            // if (fftParam.numchannels >= 1) {
            //     for (int s = 0; s < windowSize; s++) {
            //         float totalChannelData = 0f;
            //         for (int c = 0; c < fftParam.numchannels; c++) {
            //             totalChannelData += fftParam.spectrum[c][s];
            //         }
            //         Samples[s] = totalChannelData / fftParam.numchannels;
            //     }
            // }
            //
            // return fftParam.numchannels >= 1;
            return false;
        }
    }
}