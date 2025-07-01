using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Adds loot to the location.")]
    public class SearchAttachment : MonoBehaviour, IAttachmentSpec {
        public LootTableWrapper lootTableWrapper;
        public List<ItemSpawningData> additionalItemsFromBerlin;

        public Element SpawnElement() {
            return new SearchAction(lootTableWrapper.LootTable(this).Yield(),
                additionalItemsFromBerlin,
                this);
        }

        public bool IsMine(Element element) {
            return element is SearchAction;
        }
    }
}