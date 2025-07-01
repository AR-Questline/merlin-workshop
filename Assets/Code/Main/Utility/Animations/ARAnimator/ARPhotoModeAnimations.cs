using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [CreateAssetMenu(menuName = "TG/Animancer/PhotoModeAnimations", order = 0)]
    public class ARPhotoModeAnimations : ScriptableObject {
        [SerializeField] List<AnimationClip> animationClips = new();
        
        public int Count => animationClips.Count;
        
        public AnimationClip this[int index] => animationClips.ElementAtOrDefault(index);
    }
}