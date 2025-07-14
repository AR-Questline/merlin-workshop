using System;
using Awaken.TG.Assets;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    [Serializable]
    public struct GestureData {
        public string gestureStoryKey;
        [ReadOnly] public float animationLength;
        [AnimationClipAssetReference, OnValueChanged(nameof(UpdateAnimationClipLength))]
        public ShareableARAssetReference animationClipRef;

        AsyncOperationHandle<AnimationClip> _preloadedClip;

        public void Preload() {
            if (_preloadedClip.IsValid()) {
                return;
            }
            
            _preloadedClip = animationClipRef.PreloadLight<AnimationClip>();
        }

        public void Unload() {
            if (!_preloadedClip.IsValid()) {
                return;
            }
            
            animationClipRef.ReleasePreloadLight(_preloadedClip);;
            _preloadedClip = default;
        }

#if UNITY_EDITOR
        public void UpdateAnimationClipLength() {
            if (animationClipRef is not { IsSet: true }) {
                animationLength = 0;
            }

            var assets =
                UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(animationClipRef.AssetGUID));
            foreach (var asset in assets) {
                if (asset is AnimationClip animationClip && animationClip.name == animationClipRef.SubObject) {
                    animationLength = animationClip.length;
                    break;
                }
            }
        }
#else 
        public void UpdateAnimationClipLength() { }
#endif
    }
}