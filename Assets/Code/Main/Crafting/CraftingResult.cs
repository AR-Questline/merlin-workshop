using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.Crafting {
    public struct CraftingResult {
        public readonly Item item;
        public readonly CraftingResultQuality quality;

        public CraftingResult(Item item) {
            this.item = item;
            this.quality = CraftingResultQuality.Regular;
        }
        
        public CraftingResult(Item item, CraftingResultQuality quality) {
            this.item = item;
            this.quality = quality;
        }
    }
    
    public enum CraftingResultQuality : byte {
        Poor,
        Regular,
        Great
    }
}