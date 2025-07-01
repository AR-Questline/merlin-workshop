using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Spawners {
    public partial class SpawnerAction : AbstractLocationAction, IRefreshedByAttachment<SpawnerActionAttachment> {
        public override ushort TypeForSerialization => SavedModels.SpawnerAction;

        public void InitFromAttachment(SpawnerActionAttachment spec, bool isRestored) { }
        
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            foreach (var spawner in ParentModel.Elements<BaseLocationSpawner>()) {
                spawner.TryGetElement<ManualSpawner>()?.TriggerSpawner();
            }
        }
    }
}