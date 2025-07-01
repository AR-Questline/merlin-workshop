using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Pets;
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
                if (location.TryGetElement(out PetElement pet)) {
                    Interact(pet);
                }
            }
            return StepResult.Immediate;
        }

        void Interact(PetElement pet) {
            switch (interaction) {
                case Interaction.Pet:
                    pet.Pet();
                    break;
                case Interaction.Taunt:
                    pet.Taunt();
                    break;
                case Interaction.Follow:
                    pet.SetFollowing(true);
                    break;
                case Interaction.Stay:
                    pet.SetFollowing(false);
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