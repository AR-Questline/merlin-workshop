using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Choices {
    public class ItemHoverInfo : IHoverInfo {
        public string InfoGroupName { get; set; }
        public string InfoName { get; set; }
        public string InfoDescription { get; set; }
        public ShareableSpriteReference InfoIcon { get; set; }

        public ItemHoverInfo(string groupName, ItemTemplate itemTemplate) {
            InfoGroupName = groupName;
            var item = World.Add(new Item(itemTemplate));
            InfoName = item.DisplayName;
            InfoDescription = item.DescriptionFor(Hero.Current);
            InfoIcon = itemTemplate.IconReference;
            item.Discard();
        }
    }
}