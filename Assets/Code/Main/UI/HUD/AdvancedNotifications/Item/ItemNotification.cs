namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item {
    public partial class ItemNotification : AdvancedNotification {
        public ItemData itemData;
        
        public override bool IsMergeable => true;

        public ItemNotification(ItemData itemData) {
            this.itemData = itemData;
        }
        
        public void OverrideItemData(ItemData newItemData) {
            itemData = newItemData;
        }
    }
}