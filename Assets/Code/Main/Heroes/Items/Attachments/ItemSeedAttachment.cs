using Awaken.TG.Main.Heroes.Housing.Farming;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that can be planted, contains plant data.")]
    public class ItemSeedAttachment : MonoBehaviour, IAttachmentSpec {
        [field: SerializeField] public PlantSize PlantSize { get; private set; }
        [field: SerializeField] public ItemSpawningData ItemReference { get; private set; }
        [field: SerializeField] public PlantStage[] Stages { get; private set; }
        
        public Element SpawnElement() {
            return new ItemSeed();
        }

        public bool IsMine(Element element) {
            return element is ItemSeed;
        }
    }
}