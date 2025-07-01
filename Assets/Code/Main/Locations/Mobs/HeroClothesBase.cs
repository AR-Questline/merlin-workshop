using Awaken.TG.Main.Heroes.Combat;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Mobs {
    public abstract partial class HeroClothesBase : CharacterClothes {
        protected sealed override Animator FindAnimator() {
            var heroController = ParentModel.View<VHeroController>();
            if (heroController == null) {
                Log.Important?.Error($"Failed to find VHeroController in character: {ParentModel.Name}");
                return null;
            }

            return ExtractAnimator(heroController);
        }

        protected abstract Animator ExtractAnimator(VHeroController heroController);
    }
}
