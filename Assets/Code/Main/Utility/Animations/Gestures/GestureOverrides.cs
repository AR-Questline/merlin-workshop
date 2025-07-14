using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    [Serializable]
    public class GestureOverrides {
        [SerializeField] GestureData[] gestures;
        int _counter;
        
        Dictionary<string, ShareableARAssetReference> InitGestures() {
            return gestures.ToDictionary(k => k.gestureStoryKey, v => v.animationClipRef, StringComparer.InvariantCultureIgnoreCase);
        }

        public GestureData? TryToGetAnimationClipRef(string key) {
            for (int i = 0; i < gestures.Length; i++) {
                if (gestures[i].gestureStoryKey == key) {
                    return gestures[i];
                }
            }
            return null;
        }

        public void Preload() {
            if (_counter <= 0) {
                for (int index = 0; index < gestures.Length; index++) {
                    gestures[index].Preload();
                }
            }

            _counter++;
        }

        public void Unload() {
            _counter--;
            if (_counter < 0) {
                Log.Important?.Error($"Unloading gestures that are already unloaded. Validate your code. {this}");
                _counter = 0;
            }
            
            if (_counter == 0) {
                for (int index = 0; index < gestures.Length; index++) {
                    gestures[index].Unload();
                }
            }
        }

#if UNITY_EDITOR
        [Sirenix.OdinInspector.Button]
        void UpdateAnimationClipsLength() {
            for (int i = 0; i < gestures.Length; i++) {
                gestures[i].UpdateAnimationClipLength();
            }
        }
#endif
    }
}