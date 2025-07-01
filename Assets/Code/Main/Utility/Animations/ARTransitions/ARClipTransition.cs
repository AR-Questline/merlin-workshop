using System;
using Animancer;
using UnityEngine;

#if UNITY_EDITOR
using Awaken.TG.EditorOnly.Utils;
#endif

namespace Awaken.TG.Main.Utility.Animations.ARTransitions {
    [Serializable]
    public class ARClipTransition : ClipTransition {
        public float RootRotationDelta;
        
#if UNITY_EDITOR
        [SerializeField, HideInInspector] AnimationClip lastRootRotationDeltaSampleClip;

        public void ValidateAnimationClipProperty() {
            if (lastRootRotationDeltaSampleClip != Clip) {
                SampleRootRotationDelta();
            }
        }
        
        public void SampleRootRotationDelta() {
            if (!Clip) {
                RootRotationDelta = 0f;
                return;
            }
            lastRootRotationDeltaSampleClip = Clip;
            RootRotationDelta = Clip.MeasureRootEulerAnglesDelta().y;
        }
#endif
    }
}