using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public static class AudioSourceExtension {
        static Dictionary<AudioSource, Sequence> _fadeOutTweens = new Dictionary<AudioSource, Sequence>();
        static Dictionary<AudioSource, Sequence> _fadeInTweens = new Dictionary<AudioSource, Sequence>();
        
        [UnityEngine.Scripting.Preserve]
        public static Sequence FadeOut(this AudioSource audioSource, float time) {
            if (_fadeInTweens.ContainsKey(audioSource)) {
                _fadeInTweens[audioSource].Kill();
                _fadeInTweens.Remove(audioSource);
            }

            Sequence t = DOTween.Sequence()
                .Append(DOTween.To(() => audioSource.volume, v => audioSource.volume = v, 0, time))
                .AppendCallback(() => audioSource.mute = true)
                .OnComplete(() => {
                    if (_fadeOutTweens.ContainsKey(audioSource)) {
                        _fadeOutTweens.Remove(audioSource);
                    }
                });
            _fadeOutTweens[audioSource] = t;
            return t;
        }

        [UnityEngine.Scripting.Preserve]
        public static Sequence FadeIn(this AudioSource audioSource, float time) {
            if (_fadeOutTweens.ContainsKey(audioSource)) {
                _fadeOutTweens[audioSource].Kill();
                _fadeOutTweens.Remove(audioSource);
            }

            Sequence t = DOTween.Sequence()
                .AppendCallback(() => audioSource.mute = false)
                .Append(DOTween.To(() => audioSource.volume, v => audioSource.volume = v, 1, time))
                .OnComplete(() => {
                    if (_fadeInTweens.ContainsKey(audioSource)) {
                        _fadeInTweens.Remove(audioSource);
                    }
                });
            _fadeInTweens[audioSource] = t;
            return t;
        }

        [UnityEngine.Scripting.Preserve]
        public static AudioSource Copy(this AudioSource audioSource, GameObject parent) {
            AudioSource newAudioSource = parent.AddComponent<AudioSource>();
            
            newAudioSource.clip = audioSource.clip;
            newAudioSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            newAudioSource.mute = audioSource.mute;
            newAudioSource.bypassEffects = audioSource.bypassEffects;
            newAudioSource.bypassListenerEffects = audioSource.bypassListenerEffects;
            newAudioSource.bypassReverbZones = audioSource.bypassReverbZones;
            newAudioSource.playOnAwake = audioSource.playOnAwake;
            newAudioSource.loop = audioSource.loop;
            newAudioSource.priority = audioSource.priority;
            newAudioSource.volume = audioSource.volume;
            newAudioSource.pitch = audioSource.pitch;
            newAudioSource.panStereo = audioSource.panStereo;
            newAudioSource.spatialBlend = audioSource.spatialBlend;
            newAudioSource.reverbZoneMix = audioSource.reverbZoneMix;

            newAudioSource.dopplerLevel = audioSource.dopplerLevel;
            newAudioSource.spread = audioSource.spread;
            newAudioSource.rolloffMode = audioSource.rolloffMode;
            newAudioSource.minDistance = audioSource.minDistance;
            newAudioSource.maxDistance = audioSource.maxDistance;
            foreach (AudioSourceCurveType curveType in Enum.GetValues(typeof(AudioSourceCurveType))) {
                newAudioSource.SetCustomCurve(curveType, audioSource.GetCustomCurve(curveType));
            }

            return newAudioSource;
        }
    }
}
