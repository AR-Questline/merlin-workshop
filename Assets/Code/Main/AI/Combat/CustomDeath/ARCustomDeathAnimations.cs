using Awaken.TG.Assets;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Utility.Animations.ARAnimator;

namespace Awaken.TG.Main.AI.Combat.CustomDeath {
    public class ARCustomDeathAnimations : ARAnimancerCustomAnimations {
        ARStateToAnimationMapping _mapping;
        
        public ARCustomDeathAnimations(ARNpcAnimancer npcAnimancer, ShareableARAssetReference animations) : base(npcAnimancer, animations) { }
        public ARCustomDeathAnimations(ARNpcAnimancer npcAnimancer, ARAssetReference animations) : base(npcAnimancer, animations) { }

        public void ApplyOverrides() {
            var arAnimancer = ExtractAnimancer();
            if (arAnimancer == null) {
                return;
            }
            arAnimancer.ApplyOverrides(this, _mapping);
        }
        
        protected override void OnOverridesLoaded(ARAsyncOperationHandle<ARStateToAnimationMapping> _) {
            _mapping = _loadingHandle.Result;
        }
    }
}
