using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemFromDlc : Element<Item>, IRefreshedByAttachment<ItemFromDlcSpec> {
        public override ushort TypeForSerialization => SavedModels.ItemFromDlc;

        ItemFromDlcSpec _spec;
        bool _enabled;

        public bool IsVisible => _enabled;
        
        public void InitFromAttachment(ItemFromDlcSpec spec, bool isRestored) {
            _spec = spec;
            _enabled = SocialService.Get.HasDlc(_spec.RequiredDlcCategory);
        }

        protected override void OnFullyInitialized() {
            if (_enabled) {
                return;
            }

            Disable();
        }

        void Disable() {
            var item = ParentModel;
            item.SetHiddenOnUI(true);
            if (item.IsEquipped) {
                item.CharacterInventory.Unequip(item);
            }
        }
    }
}