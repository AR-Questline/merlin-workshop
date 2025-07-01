using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes.Tags;
using Cysharp.Threading.Tasks;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/Interactions: Perform and Wait for End"), NodeSupportsOdin]
    public class SEditorPerformInteractionAndWait : EditorStep {
        public LocationReference locations;
        public bool dropToAnchor = true;
        
        [Tags(TagsCategory.InteractionID)] 
        public string uniqueID;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPerformInteractionAndWait {
                locations = locations,
                dropToAnchor = dropToAnchor,
                uniqueID = uniqueID
            };
        }
    }

    public partial class SPerformInteractionAndWait : StoryStep {
        public LocationReference locations;
        public bool dropToAnchor = true;
        public string uniqueID;
        
        public override StepResult Execute(Story story) {
            var searchable = new InteractionUniqueFinder(uniqueID).Searchable;
            if (searchable == null) {
                return StepResult.Immediate;
            }
            
            foreach (var location in locations.MatchingLocations(story)) {
                var behaviours = location.TryGetElement<NpcElement>()?.Behaviours;
                if (behaviours == null) {
                    continue;
                }

                StepResult result = new StepResult();
                var interaction = InteractionProvider.GetInteraction(behaviours.Npc, searchable);
                interaction.OnInternalEnd += () => result.Complete();;
                if (dropToAnchor) {
                    behaviours.DropToAnchor().Forget();
                }
                behaviours.PushToStack(interaction);
                return result;
            }

            return StepResult.Immediate;
        }
    }
}