using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public abstract partial class ItemOnTakeFromDummyBase : Element<Item> {
        WeakModelRef<NpcDummy> _dummy;
        
        protected override void OnInitialize() {
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.AfterAttached, AfterOwnerAttached, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, Item.Events.QuantityChanged, this, OnStackableItemPickedUp);
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
                OnTakenFromDummy(dummy);
                _dummy = null;
            }
        }
        
        void OnStackableItemPickedUp(QuantityChangedData data) {
            if (data.amount < 0) {
                return;
            }
            if (!data.target.Template.Equals(ParentModel.Template)) {
                return;
            }
            
            if (_dummy.TryGet(out var dummy) && ParentModel.Owner != dummy.ParentModel) {
                OnTakenFromDummy(dummy);
                _dummy = null;
            }
        }

        protected abstract void OnTakenFromDummy(NpcDummy dummy);
    }
}