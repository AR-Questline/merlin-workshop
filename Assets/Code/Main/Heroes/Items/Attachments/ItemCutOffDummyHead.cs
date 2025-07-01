using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemCutOffDummyHead : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.ItemCutOffDummyHead;

        WeakModelRef<NpcDummy> _dummy;
        
        protected override void OnInitialize() {
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.AfterAttached, AfterOwnerAttached, this);
        }
        
        void AfterOwnerAttached() {
            NpcDummy dummy;
            
            if (_dummy.Get() == null) {
                if (ParentModel.Owner is Location location && location.TryGetElement(out dummy)) {
                    _dummy = new WeakModelRef<NpcDummy>(dummy);
                }
                return;
            }
            
            if (_dummy.TryGet(out dummy) && ParentModel.Owner != dummy.ParentModel) {
                dummy.AddElement(new DummyHeadCutOff());
                this.Discard();
            }
        }
    }
}