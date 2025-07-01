using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Starts a fireplace UI when interacted with, used by wyrd-repelling fire.")]
    public class StartWyrdrepellingFireplaceAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] List<CraftingTabsConfig> tabsToOpen;
        [SerializeField] List<CraftingTabsConfig> alchemyTabsToOpen;
        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference foredwellerLocation;
        [SerializeField] StoryBookmark foredwellerDialogue;
        [SerializeField] StoryBookmark foredwellerDialogueTester;
        [SerializeField] bool manualRestTime;
        [SerializeField] bool isUpgraded;
        
        public TabSetConfig TabSetSetConfig => new(tabsToOpen.ToDictionary(k => k.enumRef.EnumAs<CraftingTabTypes>(), v => v.tempRef.Get<CraftingTemplate>()));
        public TabSetConfig AlchemyTabSetSetConfig => new(alchemyTabsToOpen.ToDictionary(k => k.enumRef.EnumAs<CraftingTabTypes>(), v => v.tempRef.Get<CraftingTemplate>()));
        public LocationTemplate ForedwellerLocationTemplate => foredwellerLocation.Get<LocationTemplate>(this);
        public StoryBookmark ForedwellerDialogue => foredwellerDialogue;
        public StoryBookmark ForedwellerDialogueTester => foredwellerDialogueTester;
        public bool ManualRestTime => manualRestTime;
        public bool IsUpgraded => isUpgraded;

        public Element SpawnElement() {
            return new StartWyrdRepellingFireplaceAction();
        }
        
        public bool IsMine(Element element) => element is StartWyrdRepellingFireplaceAction;
        
        [Serializable]
        class CraftingTabsConfig {
            [RichEnumExtends(typeof(CraftingTabTypes))]
            public RichEnumReference enumRef;
            [TemplateType(typeof(CraftingTemplate))]
            public TemplateReference tempRef;
        }
    }
}