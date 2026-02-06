using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items.Attachments;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem {
    public partial class SpecialItemNotification : AdvancedNotification {
        public readonly bool isReadable;
        public readonly string displayName;
        public readonly SpriteReference itemIcon;
        public readonly Heroes.Items.Item item;

        public override bool IsValid => !item.HasBeenDiscarded;
        public override bool IsMergeable => true;
        
        public SpecialItemNotification(Heroes.Items.Item item) {
            this.isReadable = item.HasElement<ItemRead>();
            this.displayName = item.DisplayName;
            this.itemIcon = item.Icon.Get();
            this.item = item;
        }
    }
}