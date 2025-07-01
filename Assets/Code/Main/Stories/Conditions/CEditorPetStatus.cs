using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using JetBrains.Annotations;

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
            if (TryGetPet(story, out var pet)) {
                if (statusToCheck == Status.FollowsHero) {
                    return pet.ShouldFollowTarget && pet.TargetToFollow == Hero.Current;
                }
            }
            return false;
        }


        bool TryGetPet([CanBeNull] Story story, out PetElement pet) {
            foreach (var location in locationRef.MatchingLocations(story)) {
                if (location.TryGetElement(out pet)) return true;
            }

            pet = null;
            return false;
        }
        
        [System.Serializable]
        public enum Status : byte {
            FollowsHero
        }
    }
}