using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For housing furniture items, contains furniture location template for spawning.")]
    public class ItemFurnitureAttachment : MonoBehaviour, IAttachmentSpec {
        [TemplateType(typeof(LocationTemplate))]
        public TemplateReference furnitureTemplateRef;
        
        public Element SpawnElement() {
            return new ItemFurniture();
        }

        public bool IsMine(Element element) {
            return element is ItemFurniture;
        }

        [Button]
        void TryToAssignIconToItem() {
            var objectIcon = furnitureTemplateRef.Get<LocationTemplate>().GetComponent<IconizedObject>().IconReference;
            GetComponent<ItemTemplate>().iconReference = objectIcon;
        }
    }
}