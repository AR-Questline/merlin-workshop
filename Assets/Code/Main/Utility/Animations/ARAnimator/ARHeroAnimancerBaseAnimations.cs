using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [CreateAssetMenu(menuName = "TG/Animancer/HeroAnimancerBaseAnimations", order = 0)]
    public class ARHeroAnimancerBaseAnimations : ScriptableObject {
        public ARHeroStateToAnimationMapping[] animationMappings = Array.Empty<ARHeroStateToAnimationMapping>();
    }
}