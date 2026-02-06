using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item {
    public readonly struct ItemData {
        public readonly string itemName;
        public readonly int quantity;
        public readonly bool gain;
        public readonly Color color;
        public readonly char changeSign;
        public readonly ShareableSpriteReference itemIcon;
        public readonly ItemTemplate itemTemplate;
        
        public ItemData(string itemName, int quantity) {
            this.itemName = itemName;
            this.quantity = quantity;
            this.gain = quantity >= 0;
            this.color = ARColor.MainGrey;
            this.changeSign = gain ? '+' : ' ';
            this.itemIcon = null;
            this.itemTemplate = null;
        }

        public ItemData(ItemTemplate itemTemplate, int quantity) {
            this.itemName = itemTemplate.ItemName;
            this.quantity = quantity;
            this.gain = quantity >= 0;
            this.color = gain ? ARColor.MainGrey : ARColor.MainRed;
            this.changeSign = 'x';
            this.itemIcon = itemTemplate.IconReference();
            this.itemTemplate = itemTemplate;
        }
    }
}