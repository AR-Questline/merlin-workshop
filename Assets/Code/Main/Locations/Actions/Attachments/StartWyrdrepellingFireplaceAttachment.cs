using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Starts a fireplace UI when interacted with, used by wyrd-repelling fire.")]
    public class StartWyrdrepellingFireplaceAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] List<CraftingTabsConfig> tabsToOpen;
        [SerializeField] List<CraftingTabsConfig> alchemyTabsToOpen;
        [SerializeField] bool manualRestTime;
        [SerializeField] bool isUpgraded;
        [SerializeField] FireplaceTalkConfig[] talkConfigs = Array.Empty<FireplaceTalkConfig>();
        public TabSetConfig TabSetSetConfig => new(tabsToOpen.ToDictionary(k => k.enumRef.EnumAs<CraftingTabTypes>(), v => v.tempRef.Get<CraftingTemplate>()));
        public TabSetConfig AlchemyTabSetSetConfig => new(alchemyTabsToOpen.ToDictionary(k => k.enumRef.EnumAs<CraftingTabTypes>(), v => v.tempRef.Get<CraftingTemplate>()));
        public FireplaceTalkConfig[] TalkConfigs => talkConfigs;
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

        [Serializable]
        public struct FireplaceTalkConfig {
            [TemplateType(typeof(LocationTemplate))] public TemplateReference talkingLocation;
            public float spawnDistance;
            public StoryBookmark dialogue;
            public StoryBookmark dialogueTester;
            public bool hideAfterNoTalkOptions;
            [Tags(TagsCategory.Flag)] public string[] requiredFlags;
            public bool requireDLC;
            [ShowIf(nameof(requireDLC))] public DlcCategory requiredDLC;
            [Tags(TagsCategory.Flag)] public string disablingFlag;
        }
    }
}