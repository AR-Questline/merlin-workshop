using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem {
    public partial class SpecialItemNotification : Element<SpecialItemNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly bool isReadable;
        public readonly string displayName;
        public readonly SpriteReference itemIcon;
        public readonly Heroes.Items.Item item;

        bool IAdvancedNotification.IsValid => !item.HasBeenDiscarded;

        public SpecialItemNotification(Heroes.Items.Item item) {
            this.isReadable = item.HasElement<ItemRead>();
            this.displayName = item.DisplayName;
            this.itemIcon = item.Icon.Get();
            this.item = item;
        }
    }
}