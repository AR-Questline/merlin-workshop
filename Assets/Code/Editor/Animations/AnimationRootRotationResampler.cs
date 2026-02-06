using System.Collections.Generic;
using Animancer;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.ARTransitions;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Animations {
    public static class AnimationRootRotationResampler {
        [MenuItem("TG/Animations/Resample Horizontal Root Rotation For All Animation Overrides")]
        static void Convert() {
            ResampleARStateToAnimationMappings();
            ResampleMovementMixers();
        }

        static void ResampleARStateToAnimationMappings() {
            foreach (var mapping in IterateOverAssets<ARStateToAnimationMapping>()) {
                mapping.EDITOR_ResampleHorizontalRootRotation();
                EditorUtility.SetDirty(mapping);
            }
        }

        static void ResampleMovementMixers() {
            foreach (var mixer in IterateOverAssets<MixerTransition2DAsset>()) {
                if (mixer.Transition is ARMixerTransition arMixer) {
                    arMixer.EDITOR_ResampleHorizontalRootRotation();
                    EditorUtility.SetDirty(mixer);
                }
            }
        }

        static IEnumerable<T> IterateOverAssets<T>() where T : ScriptableObject {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, typeof(T));
                if (asset is T typedAsset) {
                    yield return typedAsset;
                }
            }
        }
    }
}