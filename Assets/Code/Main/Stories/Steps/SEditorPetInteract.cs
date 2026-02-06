using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Locations.Pets.Variants;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Pet: Interact")]
    public class SEditorPetInteract : EditorStep {
        public LocationReference locationRef = new() { targetTypes = TargetType.Self };
        public SPetInteract.Interaction interaction = SPetInteract.Interaction.Nothing;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPetInteract {
                locationRef = locationRef,
                interaction = interaction
            };
        }
    }
    
    public partial class SPetInteract : StoryStep {
        public LocationReference locationRef = new() { targetTypes = TargetType.Self };
        public Interaction interaction = Interaction.Nothing;
        
        public override StepResult Execute(Story story) {
            foreach (var location in locationRef.MatchingLocations(story)) {
                if (location.TryGetElement<PetVariantBase>(out var pet)) {
                    InteractWithPetVariant(pet);
                }
            }
            return StepResult.Immediate;
        }

        void InteractWithPetVariant(PetVariantBase petVariant) {
            switch (interaction) {
                case Interaction.Pet:
                    petVariant.PerformPetting();
                    break;
                case Interaction.Taunt:
                    petVariant.PerformTaunt();
                    break;
                case Interaction.Follow:
                    petVariant.SetFollowing(true);
                    break;
                case Interaction.Stay:
                    petVariant.SetFollowing(false);
                    break;
            }
        }
        
        [System.Serializable]
        public enum Interaction : byte {
            Nothing,
            Pet,
            Taunt,
            Stay,
            Follow,
        }
    }
}