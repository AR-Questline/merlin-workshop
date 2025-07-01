using System;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    [Serializable]
    public struct GestureData {
        public string gestureStoryKey;
        public AnimationClip animationClip;
    }
}