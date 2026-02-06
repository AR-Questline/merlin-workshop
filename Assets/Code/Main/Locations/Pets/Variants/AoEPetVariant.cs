using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Locations.Pets.Variants {
    public partial class AoEPetVariant : PetVariant {
        public override ushort TypeForSerialization => SavedModels.AoEPetVariant;
        
        PersistentAoE _persistentAoE;
        PersistentAoE PersistentAoE => ParentModel.TryGetCachedElement(ref _persistentAoE);
        
        protected override void OnSpawned() {
            if (PersistentAoE == null) {
                Log.Important?.Error($"AoEPetVariant {ParentModel} spawned without PersistentAoE.");
                return;
            }
            
            PersistentAoE.AssignDamageDealer(Pet.Owner);
        }

        protected override void OnBeforeEnd() {
            base.OnBeforeEnd();

            if (PersistentAoE is { HasBeenDiscarded: false }) {
                PersistentAoE?.Duration.Discard();
            }
        }
    }
}