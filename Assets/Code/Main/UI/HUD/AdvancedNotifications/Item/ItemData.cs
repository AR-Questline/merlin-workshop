using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item {
    public readonly struct ItemData {
        public readonly string itemName;
        public readonly int? quantity;
        public readonly Color color;
        public readonly char changeSign;
        public readonly ShareableSpriteReference itemIcon;
        
        public ItemData(string itemName, int? quantity, Color color, char changeSign = 'x') {
            this.itemName = itemName;
            this.quantity = quantity;
            this.color = color;
            this.changeSign = changeSign;
            this.itemIcon = null;
        }

        public ItemData(ItemTemplate itemTemplate, int? quantity, Color color, char changeSign = 'x') {
            this.itemName = itemTemplate.ItemName;
            this.quantity = quantity;
            this.color = color;
            this.changeSign = changeSign;
            this.itemIcon = itemTemplate.IconReference;
        }
    }
}