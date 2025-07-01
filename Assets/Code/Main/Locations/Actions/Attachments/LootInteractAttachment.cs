using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [RequireComponent(typeof(AliveLocationAttachment))]
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Used by locations that give loot when they get hit (mine).")]
    public class LootInteractAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, RichEnumExtends(typeof(ToolType))] 
        RichEnumReference toolTypeRequired;
        public LootTableWrapper lootTable;

        public ToolType ToolType => toolTypeRequired.EnumAs<ToolType>();
        
        public Element SpawnElement() {
            return new LootInteractAction();
        }

        public bool IsMine(Element element) => element is LootInteractAction;
    }
}