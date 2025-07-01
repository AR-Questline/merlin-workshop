using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Gliding;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class GliderItem : Element<Item>, IRefreshedByAttachment<GliderAttachment> {
        public override ushort TypeForSerialization => SavedModels.GliderItem;

        Item Item => ParentModel;
        Hero HeroOwner => Item.Owner?.Character as Hero;
        
        public HeroGlideAction OwnActiveAction { get; private set; }
        public GliderAttachment Attachment { get; private set; }

        protected override void OnInitialize() {
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.AfterEstablished, OnLearn, this);
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.BeforeDisestablished, OnForget, this);
            TryAttachingActionToOwner();
        }
        
        public void InitFromAttachment(GliderAttachment spec, bool isRestored) {
            Attachment = spec;
        }
        
        void OnLearn(RelationEventData _) {
            TryAttachingActionToOwner();
        }
        
        void OnForget(RelationEventData _) {
            DetachOwnActiveAction();
        }

        void TryAttachingActionToOwner() {
            if (HeroOwner == null) {
                return;
            }

            if (HeroOwner.TryGetElement(out HeroGlideAction glideAction)) {
                if (glideAction.Glider.Item.Quality < Item.Quality) {
                    HeroOwner.RemoveElement(glideAction);
                } else {
                    return;
                }
            }

            OwnActiveAction = new HeroGlideAction(this);
            HeroOwner.AddElement(OwnActiveAction);
        }

        void DetachOwnActiveAction() {
            if (OwnActiveAction) {
                OwnActiveAction.Discard();
                OwnActiveAction = null;
            }
        }
    }
}