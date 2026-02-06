using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Locations.Pets.Variants;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if pet has given status
    /// </summary>
    [Element("Pet: Status")]
    public class CEditorPetStatus : EditorCondition {
        public LocationReference locationRef = new() { targetTypes = TargetType.Self };
        public CPetStatus.Status statusToCheck;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CPetStatus {
                locationRef = locationRef,
                statusToCheck = statusToCheck,
            };
        }
    }
    
    public partial class CPetStatus : StoryCondition {
        public LocationReference locationRef;
        public Status statusToCheck;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            foreach (var location in locationRef.MatchingLocations(story)) {
                if (FulfilledForLocation(location)) {
                    return true;
                }
            }
            return false;
        }

        bool FulfilledForLocation(Location location) {
            if (location.TryGetElement<PetElement>(out var pet)) {
                if (statusToCheck == Status.FollowsHero) {
                    return pet.WantsToFollowTarget && pet.TargetToFollow == Hero.Current;
                }
            }
            
            if (location.TryGetElement<PetVariantBase>(out var petVariant)) {
                if (statusToCheck == Status.IsNonPetVariant) {
                    return petVariant is not PetVariant;
                }
            }

            return false;
        }
        
        [System.Serializable]
        public enum Status : byte {
            FollowsHero,
            IsNonPetVariant,
        }
    }
}