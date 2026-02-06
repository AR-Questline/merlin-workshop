using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Locations.Pets.Variants;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Pet: Set Variant")]
    public class SEditorPetSetVariant : EditorStep {
        public LocationReference locationRef = new() { targetTypes = TargetType.Self };
        public TemplateReference variantTemplateRef;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPetSetVariant {
                locationRef = locationRef,
                variantTemplateRef = variantTemplateRef,
            };
        }
    }
    
    public partial class SPetSetVariant : StoryStep {
        public LocationReference locationRef = new() { targetTypes = TargetType.Self };
        public TemplateReference variantTemplateRef;
        
        public override StepResult Execute(Story story) {
            if (TryGetPetFromMatchingLocations(story, out var pet)) {
                pet.StartVariantFeedSequence(variantTemplateRef);
            }
            return StepResult.Immediate;
        }

        bool TryGetPetFromMatchingLocations(Story story, out PetVariantBase pet) {
            foreach (var location in locationRef.MatchingLocations(story)) {
                if (location.TryGetElement(out pet)) {
                    return true;
                }
            }
            
            pet = null;
            return false;
        }
    }
}