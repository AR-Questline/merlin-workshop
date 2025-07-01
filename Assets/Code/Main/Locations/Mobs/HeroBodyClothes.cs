using Awaken.TG.Main.Heroes.Combat;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Mobs {
    public sealed partial class HeroBodyClothes : HeroClothesBase {
        protected override Animator ExtractAnimator(VHeroController heroController) {
            return heroController.HeroAnimator;
        }
    }
}