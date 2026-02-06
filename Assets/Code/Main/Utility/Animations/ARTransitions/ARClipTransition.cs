using System;
using Animancer;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using Awaken.TG.EditorOnly.Utils;
#endif

namespace Awaken.TG.Main.Utility.Animations.ARTransitions {
    [Serializable]
    public class ARClipTransition : ClipTransition {
        [SerializeField, ReadOnly] float[] _rootRotationSamples = Array.Empty<float>();
        public float TargetRootRotation => _rootRotationSamples.Length > 0 ? _rootRotationSamples[^1] : 0f;

        public float GetRootRotationAtTime(float time) {
            if (_rootRotationSamples.Length == 0 || Clip == null || time <= 0f) {
                return 0f;
            }
            
            float clipLength = Clip.length;
            float timeToFrames = (_rootRotationSamples.Length - 1f) / clipLength;
            float framePosition = time * timeToFrames;

            int maxIndex = _rootRotationSamples.Length - 1;
            int lowerIndex = (int)math.clamp(math.floor(framePosition), 0, maxIndex);
            int higherIndex = (int)math.clamp(math.ceil(framePosition), 0, maxIndex);
            
            float sampleLerpFactor = math.fmod(framePosition, 1f);
            float lowerSample = _rootRotationSamples[lowerIndex];
            float higherSample = _rootRotationSamples[higherIndex];
            
            return math.lerp(lowerSample, higherSample, sampleLerpFactor);
        }
        
        public float GetRootRotationDelta(float time, float duration) {
            return GetRootRotationAtTime(time + duration) - GetRootRotationAtTime(time);
        }
        
        
#if UNITY_EDITOR
        [SerializeField, HideInInspector] AnimationClip lastRootRotationSampleClip;

        public void ValidateAnimationClipProperty() {
            if (lastRootRotationSampleClip != Clip) {
                SampleRootHorizontalRotation();
            }
        }
        
        public void SampleRootHorizontalRotation() {
            if (!Clip) {
                _rootRotationSamples = Array.Empty<float>();
                return;
            }
            lastRootRotationSampleClip = Clip;
            _rootRotationSamples = Clip.SampleRootHorizontalRotation();
        }
#endif
    }
}