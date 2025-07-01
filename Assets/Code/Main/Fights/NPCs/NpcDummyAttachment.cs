using System;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.List;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Defines a dummy NPC - not alive, but can wear items.")]
    public class NpcDummyAttachment : MonoBehaviour, IAttachmentSpec {
        [TemplateType(typeof(NpcTemplate))] 
        public TemplateReference npcTemplate;
        [List(ListEditOption.Buttons | ListEditOption.ListLabel)]
        public ItemSpawningData[] initialItems = Array.Empty<ItemSpawningData>();

        public Element SpawnElement() {
            return new NpcDummy(npcTemplate.Get<NpcTemplate>(), initialItems);
        }

        public bool IsMine(Element element) => element is NpcDummy;
    }
}
