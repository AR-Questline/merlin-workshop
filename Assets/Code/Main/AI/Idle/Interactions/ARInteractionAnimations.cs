using Awaken.TG.Assets;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Utility.Animations.ARAnimator;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class ARInteractionAnimations : ARNpcCustomAnimations {
        public InteractionAnimationData InteractionAnimationData { get; private set; } = InteractionAnimationData.Default();
        
        public ARInteractionAnimations(NpcElement npc, ShareableARAssetReference animations) : base(npc, animations) { }
        
        protected override void OnOverridesLoaded(ARAsyncOperationHandle<ARStateToAnimationMapping> _) {
            var arAnimancer = ExtractAnimancer();
            if (arAnimancer == null) {
                return;
            }
            var mapping = _loadingHandle.Result;
            if (mapping is ARStateToInteractionAnimationMapping interactionMapping) {
                InteractionAnimationData = interactionMapping.interactionData;
            }
            arAnimancer.ApplyOverrides(this, mapping);
        }
    }
}
