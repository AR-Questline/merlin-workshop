using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class KillSelfOnInteract : Element<Location>, IRefreshedByAttachment<KillSelfOnInteractAttachment> {
        public override ushort TypeForSerialization => SavedModels.KillSelfOnInteract;

        KillSelfOnInteractAttachment _spec;

        public void InitFromAttachment(KillSelfOnInteractAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(Location.Events.Interacted, OnInteract, this);
        }

        void OnInteract() {
            if (_spec.makeInactive) {
                ParentModel.SetInteractability(LocationInteractability.Inactive);
            }
            ParentModel.TryGetElement<IAlive>()?.Kill();
        }
    }
}