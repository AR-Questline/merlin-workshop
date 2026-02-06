using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Pets.Variants {
    public partial class PetVariant : PetVariantBase {
        public override ushort TypeForSerialization => SavedModels.PetVariant;
        
        PetElement _petElement;
        public PetElement Pet => ParentModel.TryGetCachedElement(ref _petElement);
        protected override bool CanReduceTimeLeft => Pet is not { CanInteractWith: false };

        protected override void OnInitialize() {
            base.OnInitialize();
            SetFollowing(Pet.WantsToFollowTarget);
        }

        protected override void OnBeforeSpawn() {
            var stateToUse = TransformationVariant is MountPetVariant
                ? ARPetAnimancer.State.TransitionLarge
                : ARPetAnimancer.State.Transition;
            Pet.Controller.Animancer.PlayAnimationState(stateToUse);

            if (TransformationVariant is PetVariant otherVariant) {
                otherVariant.Pet.Controller.Animancer.SyncAnimationWith(Pet.Controller.Animancer);
            }
        }
        
        protected override void OnPet() {
            Pet.Pet();
        }

        protected override void OnTaunt() {
            Pet.Taunt();
        }

        protected override void OnFed() {
            base.OnFed();
            Pet.Controller.Animancer.PlayAnimationState(ARPetAnimancer.State.Feed);
        }

        protected override void OnFollowStateChanged(bool state) {
            Pet.SetFollowing(state);
        }

        protected override void OnBeforeEnd() {
            if (TransformationVariant is MountPetVariant) {
                Pet.Controller.Animancer.PlayAnimationState(ARPetAnimancer.State.TransitionLarge);
            }
        }
    }
}