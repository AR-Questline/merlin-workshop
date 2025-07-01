using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item {
    public partial class ItemNotification : Element<ItemNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly ItemData itemData;

        public ItemNotification(ItemData itemData) {
            this.itemData = itemData;
        }
    }
}