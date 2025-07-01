using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Starts crafting when interacted with.")]
    public class StartCraftingAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] List<CraftingTabsConfig> tabsToOpen;

        public TabSetConfig TabSetConfig => new(new Dictionary<CraftingTabTypes, CraftingTemplate>(
            tabsToOpen.Select(k => new KeyValuePair<CraftingTabTypes, CraftingTemplate>(
                k.enumRef.EnumAs<CraftingTabTypes>(), 
                k.tempRef.Get<CraftingTemplate>()))));

        public Element SpawnElement() {
            return new StartCraftingAction();
        }
        
        public bool IsMine(Element element) => element is StartCraftingAction;
        
        [Serializable]
        class CraftingTabsConfig {
            [RichEnumExtends(typeof(CraftingTabTypes))]
            public RichEnumReference enumRef;
            [TemplateType(typeof(CraftingTemplate))]
            public TemplateReference tempRef;
        }
    }
}