using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Pets.Variants;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    public static class PetUtils {
        public static void RecallPet(Vector3 coords) {
            if (PetVariantBase.TryGetCurrentlyActiveVariant(out var variant)) {
                var baseVariant = variant.TransformIntoBaseVariant(true);
                if (baseVariant != null && baseVariant.ParentModel.TryGetElement<PetElement>(out var petElement)) {
                    petElement.Recall(coords);
                    return;
                }
            }

            var pet = World.Any<PetElement>();
            if (pet != null) {
                pet.Recall(coords);
            }
        }
        
        public static bool HasPetBeenLeftBehind() {
            Location locationToCheck;
            if (PetVariantBase.TryGetCurrentlyActiveVariant(out var variant)) {
                locationToCheck = variant.ParentModel;
            } else if (World.Any<PetElement>() is { } petElement) {
                locationToCheck = petElement.ParentModel;
            } else {
                return false;
            }
            
            if (locationToCheck.TryGetElement<GameplayUniqueLocation>(out var uniqueLocation) && !uniqueLocation.InCurrentScene) {
                return true;
            }
            
            int distanceBand = locationToCheck.GetCurrentBandSafe(LocationCullingGroup.LastBand);
            if (!LocationCullingGroup.InNpcVisibilityBand(distanceBand)) {
                return true;
            }

            return false;
        }
    }
}