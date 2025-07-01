using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Common, "For items that can be used as tools.")]
    public class ToolAttachment: MonoBehaviour, IAttachmentSpec {
        [SerializeField, RichEnumExtends(typeof(ToolType))] 
        RichEnumReference toolType;

        public bool canInteractWithLightAttack;
        public bool canInteractWithHeavyAttack;
        
        public ToolType Type => toolType.EnumAs<ToolType>();
        
        public Element SpawnElement() {
            return new Tool();
        }

        public bool IsMine(Element element) => element is Tool;
    }
}