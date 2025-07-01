using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Fishing {
    public readonly struct FishCaughtData {
        public readonly string itemName;
        public readonly string fishName;
        public readonly bool isFish;
        public readonly bool isRecord;
        public readonly bool isNewFish;
        public readonly string fishWeight;
        public readonly string fishLength;
        public readonly SpriteReference itemIcon;
        public readonly SpriteReference fishIcon;
        public readonly int itemQuantity;
        public readonly string prevRecord;

        public FishCaughtData(ItemTemplate itemToEq, ItemTemplate fish, bool isFish, bool isNewFish, bool isRecord, float fishWeight, float fishLength, int itemQuantity, string prevRecord = "") {
            this.isFish = isFish;
            this.isRecord = isRecord;
            this.isNewFish = isNewFish;
            this.fishWeight = fishWeight.ToString("F1");
            this.fishLength = fishLength.ToString("F1");
            
            //Using data about catched object (if it's item, not a fish, than fish == itemToEq, otherwise fish is fish, not fish meat)
            this.itemName = itemToEq.itemName;
            this.itemIcon = itemToEq.iconReference.Get();
            this.itemQuantity = itemQuantity;
            
            this.fishName = fish.itemName;
            this.fishIcon = fish.iconReference.Get();
            this.prevRecord = prevRecord;
        }
    }
}