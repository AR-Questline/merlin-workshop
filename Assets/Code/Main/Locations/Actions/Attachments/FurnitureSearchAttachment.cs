using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Stores loot table for furniture.")]
    public class FurnitureSearchAttachment : MonoBehaviour, IAttachmentSpec {
        [Space]
        public ARTimeSpan renewLootRate;
        [Space]
        public LootTableWrapper lootTableWrapper;
        public List<ItemSpawningData> additionalItems;
        
        public Element SpawnElement() {
            return new FurnitureSearchAction();
        }
        public bool IsMine(Element element) {
            return element is FurnitureSearchAction;
        }
    }
}