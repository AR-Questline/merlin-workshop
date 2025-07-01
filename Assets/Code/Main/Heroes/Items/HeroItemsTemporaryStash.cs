using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items {
    public partial class HeroItemsTemporaryStash : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroItemsTemporaryStash;

        [Saved] public List<StashedItemData> stashedItems = new();
        List<Item> _itemsToStash = new();

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public HeroItemsTemporaryStash() { }

        public HeroItemsTemporaryStash(List<Item> itemsToStash) {
            this._itemsToStash = itemsToStash;
        }

        protected override void OnFullyInitialized() {
            foreach (var item in _itemsToStash) {
                stashedItems.Add(new StashedItemData {
                    item = item,
                    wasEquipped = item.IsEquipped,
                });
            }

            _itemsToStash = null;
        }

        public bool ContainsItem(Item item) {
            return stashedItems.Any(stashedItemData => stashedItemData.item != null && Equals(stashedItemData.item.Template, item.Template));
        }

        public partial struct StashedItemData {
            public ushort TypeForSerialization => SavedTypes.StashedItemData;

            [Saved] public Item item;
            [Saved(false)] [UnityEngine.Scripting.Preserve] public bool wasEquipped;
        }
    }
}