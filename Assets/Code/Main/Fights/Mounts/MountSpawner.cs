using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Fights.Mounts {
    public partial class MountSpawner : Element<Location>, IRefreshedByAttachment<MountSpawnerAttachment> {
        public override ushort TypeForSerialization => SavedModels.MountSpawner;

        MountSpawnerAttachment _spec;
        
        public void InitFromAttachment(MountSpawnerAttachment spec, bool isRestored) {
            _spec = spec;
        }
        
        protected override void OnInitialize() {
            if (ParentModel.Interactability != LocationInteractability.Active) {
                ParentModel.ListenTo(Location.Events.InteractabilityChanged, TrySpawnMount, this);
            } else {
                SpawnMount();
            }
        }
        
        void TrySpawnMount(LocationInteractability interactability) {
            if (interactability == LocationInteractability.Active) {
                SpawnMount();
            }
        }

        void SpawnMount() {
            var template = _spec.Template;
            var location = template.SpawnLocation(ParentModel.Coords, ParentModel.Rotation, template.transform.localScale);

            GameplayUniqueLocation.InitializeForLocation(location);
            ParentModel.Discard();
        }

        protected override void OnRestore() { }
    }
}
