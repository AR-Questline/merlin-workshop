using System.Linq;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Interact"), NodeSupportsOdin]
    public class SEditorLocationInteract : EditorStep {
        public LocationReference locationReference;
        public bool isHeroInteracting = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationInteract {
                locationReference = locationReference,
                isHeroInteracting = isHeroInteracting
            };
        }
    }

    public partial class SLocationInteract : StoryStep {
        public LocationReference locationReference;
        public bool isHeroInteracting = true;
        
        public override StepResult Execute(Story story) {
            foreach (var matchingLocation in locationReference.MatchingLocations(story).ToArray()) {
                HeroInteraction.StartInteraction(isHeroInteracting ? story.Hero : null, matchingLocation, out _);
            }
            return StepResult.Immediate;
        }
    }
}