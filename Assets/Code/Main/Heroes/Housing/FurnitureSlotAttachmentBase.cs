using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing {
    [Serializable]
    public abstract class FurnitureSlotAttachmentBase : MonoBehaviour, IAttachmentSpec {
        [FoldoutGroup("Design"), Tags(TagsCategory.Location)]
        public string[] tags = Array.Empty<string>();
        
        [LocStringCategory(Category.Housing)]
        public LocString displayName;

        [TemplateType(typeof(LocationTemplate))]
        public TemplateReference furniturePlaceholder;

        public abstract Element SpawnElement();

        public abstract bool IsMine(Element element);
    }
}