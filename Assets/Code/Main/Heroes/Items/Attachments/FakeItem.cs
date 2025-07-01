using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class FakeItem : Element<Item>, IRefreshedByAttachment<FakeItemAttachment> {
        public override ushort TypeForSerialization => SavedModels.FakeItem;

        FakeItemAttachment _spec;
        
        public void InitFromAttachment(FakeItemAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(ICharacterInventory.Events.ItemToBeAddedToInventory, OnTryingToAddToInventory, this);
        }

        void OnTryingToAddToInventory(HookResult<IModel, ICharacterInventory.AddingItemInfo> result) {
            if (_spec.RealItem == null) {
                Log.Important?.Warning("FakeItem has no real item " + LogUtils.GetDebugName(ParentModel));
                ParentModel.Discard();
                return;
            }

            ICharacterInventory characterInventory = result.Value.Inventory;
            if (characterInventory.Owner != Hero.Current) {
                return;
            }
            if (_spec.RealItem.CanStack) {
                result.Value.Item = World.Add(new Item(_spec.RealItem, ParentModel.Quantity));
            } else {
                result.Value.Item = World.Add(new Item(_spec.RealItem));
                
                if (ParentModel.Quantity > 1) {
                    Log.Important?.Error("FakeItem tried to add multiple non-stackable items to inventory! Could have unintended interactions"
                                         + "\nFake Item: " + LogUtils.GetDebugName(ParentModel)
                                         + "\nReal item: " + LogUtils.GetDebugName(_spec.RealItem));
                    for (int i = 1; i < ParentModel.Quantity; i++) {
                         characterInventory.Add(new Item(_spec.RealItem));
                    }
                }
            }
            ParentModel.Discard();
        }
    }
}