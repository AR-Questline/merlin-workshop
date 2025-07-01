using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Saving;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Mobs {
    public sealed partial class HeroHandClothes : HeroClothesBase {
        protected override Animator ExtractAnimator(VHeroController heroController) {
            return heroController.HeroAnimator;
        }
    }
}