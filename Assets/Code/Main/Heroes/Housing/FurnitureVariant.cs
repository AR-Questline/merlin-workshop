using Awaken.Utility;
using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Heroes.Housing {
    [Serializable]
    public partial struct FurnitureVariant {
        public ushort TypeForSerialization => SavedTypes.FurnitureVariant;

        [Saved] ItemTemplate _itemTemplate;
        [Saved] LocationTemplate _furnitureTemplate;

        public string[] Tags => _itemTemplate.tags;
        public ShareableSpriteReference FurnitureIcon => _itemTemplate.IconReference;
        public LocationTemplate FurnitureVariantTemplate => _furnitureTemplate;
        public string FurnitureName => _itemTemplate.itemName.Translate();
        public string FurnitureDescription => _itemTemplate.DescriptionLoc.Translate();

        public FurnitureVariant(Item item, LocationTemplate furnitureTemplate) {
            _itemTemplate = item.Template;
            _furnitureTemplate = furnitureTemplate;
        }
    }
}