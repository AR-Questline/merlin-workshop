using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    [Serializable]
    public class GestureOverrides {
        [SerializeField] List<GestureData> gestures;
        
        Dictionary<string, AnimationClip> _gestureAnimationClips; 
        public Dictionary<string, AnimationClip> GestureAnimationClips => _gestureAnimationClips ??= InitGestures();
        [UnityEngine.Scripting.Preserve] public IEnumerable<AnimationClip> AllClips => GestureAnimationClips.Values;
        
        Dictionary<string, AnimationClip> InitGestures() {
            return gestures.ToDictionary(k => k.gestureStoryKey, v => v.animationClip, StringComparer.InvariantCultureIgnoreCase);
        }

        public AnimationClip TryToGetAnimationClip(string key) {
            return GestureAnimationClips.TryGetValue(key, out AnimationClip animationClip) ? animationClip : null;
        }
    }
}