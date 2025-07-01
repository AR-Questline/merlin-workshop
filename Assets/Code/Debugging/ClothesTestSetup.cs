using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Debugging {
    public class ClothesTestSetup : MonoBehaviour {
#if UNITY_EDITOR
        public GameObject malePrefab;
        public GameObject femalePrefab;

        [ShowInInspector, ListDrawerSettings(CustomAddFunction = nameof(AddNewAnimation))]
        public PredefinedAnimationClip[] animations = Array.Empty<PredefinedAnimationClip>();

        public AnimationClip[] clipsForCulling = Array.Empty<AnimationClip>();

        PredefinedAnimationClip AddNewAnimation() => new();

        [InlineProperty, HideLabel, HideReferenceObjectPicker, Serializable]
        public class PredefinedAnimationClip {
            public event Action<AnimationClip> LoadAnimation;

            [InlineButton(nameof(Select))]
            public AnimationClip clip;
            
            public void Select() {
                LoadAnimation?.Invoke(clip);
                if (!Application.isPlaying) {
                    EditorGUIUtility.PingObject(clip);
                }
            }
        }
#endif
    }
}
