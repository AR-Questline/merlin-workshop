using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets.Variants {
    public partial class MountPetVariant : PetVariantBase {
        public override ushort TypeForSerialization => SavedModels.MountPetVariant;

        const float SpawnSafetyMargin = 0.5f;
        
        [Saved] WeakModelRef<MountElement> _previouslyOwnedMount;

        MountElement _petMountElement;
        MountElement PetMount => ParentModel.TryGetCachedElement(ref _petMountElement);

        protected override bool CanReduceTimeLeft => PetMount.MountedHero == null;
        
        protected override void OnBeforeSpawn() {
            PetMount.View<VMount>().PlayTransformAnimation();
            TeleportToSpawnPosition();
        }

        void TeleportToSpawnPosition() {
            PetMount.View<VMount>().Teleport(ParentModel.Coords + Vector3.up * SpawnSafetyMargin);
        }
        
        protected override void OnSpawned() {
            _previouslyOwnedMount = PetOwner.OwnedMount;
            PetOwner.OwnedMount = PetMount;
            PetMount.MarkAsHeroMount(true);
            
            TeleportToSpawnPosition();
        }

        protected override void OnPet() {
            PetMount.Pet();
        }

        protected override void OnFed() {
            PetMount.View<VMount>().PlayPetAnimation();
        }

        protected override void OnBeforeEnd() {
            ParentModel.RemoveElementsOfType<AbstractLocationAction>();
            PetMount.View<VMount>().PlayTransformAnimation();
            PetOwner.OwnedMount = _previouslyOwnedMount;
        }
    }
}