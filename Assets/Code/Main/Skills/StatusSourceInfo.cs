using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Skills {
    [Serializable]
    public partial class StatusSourceInfo {
        public ushort TypeForSerialization => SavedTypes.StatusSourceInfo;

        [Saved] public string SourceUniqueID { get; private set; }
        [Saved] public WeakModelRef<ICharacter> SourceCharacter { get; private set; }
        [Saved] public WeakModelRef<Item> SourceItem { get; private set; }
        [Saved] public ShareableSpriteReference Icon { get; private set; }
        [Saved(false)] public bool HiddenOnUI { get; private set; }
        [Saved(false)] public bool AlwaysShowSeparately { get; private set; }
        
        [Saved] UnicodeString DisplayName { get; set; }
        [Saved] LocString DisplayNameLoc { get; set; }
        [Saved] UnicodeString SourceDescription { get; set; }
        [Saved] LocString SourceDescriptionLoc { get; set; }

        public string DisplayNameString {
            get {
                if (DisplayNameLoc != null) {
                    return DisplayNameLoc.Translate();
                }
                string displayNameString = DisplayName?.ToString();
                if (string.IsNullOrEmpty(displayNameString)) {
                    return string.Empty;
                }
                return displayNameString;
            }
        }

        public string DescriptionString => SourceDescriptionLoc != null 
            ? SourceDescriptionLoc.Translate() 
            : string.IsNullOrEmpty(SourceDescription.ToString()) 
                ? string.Empty 
                : SourceDescription.ToString();
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public ICharacter GetSourceCharacter => SourceCharacter.Get();

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public StatusSourceInfo() { }
        
        StatusSourceInfo(StatusTemplate statusTemplate) {
            AlwaysShowSeparately = statusTemplate.alwaysShowSeparately;
            SourceUniqueID = NewGUID();
            
            DisplayNameLoc = statusTemplate.displayName;
            SourceDescriptionLoc = statusTemplate.description;
            Icon = statusTemplate.iconReference;
            HiddenOnUI = statusTemplate.hiddenOnUI;
        }

        StatusSourceInfo(Skill skill, StatusTemplate statusTemplate) {
            var item = skill.SourceItem;
            SourceItem = item;
            SourceUniqueID = item?.ID ?? NewGUID();
            
            if (skill.ParentModel is IItemSkillOwner iso) {
                string toAppendInvokeID = "_" + iso.PerformCount;
                SourceUniqueID += toAppendInvokeID;
            }
            
            SourceCharacter = new WeakModelRef<ICharacter>(skill.Owner);
            DisplayNameLoc = skill.DisplayNameLoc;
            SourceDescription = skill.SourceDescription;
            Icon = skill.Icon;
            if (Icon is not { IsSet: true }) {
                Icon = statusTemplate.iconReference;
            }
            HiddenOnUI = skill.HiddenOnUI || statusTemplate.hiddenOnUI;
        }
        
        public StatusSourceInfo(StatusSourceInfo source) {
            SourceUniqueID = source.SourceUniqueID;
            SourceCharacter = source.SourceCharacter;
            SourceItem = source.SourceItem;
            DisplayNameLoc = source.DisplayNameLoc;
            SourceDescriptionLoc = source.SourceDescriptionLoc;
            Icon = source.Icon;
            HiddenOnUI = source.HiddenOnUI;
        }
        
        public static StatusSourceInfo FromStatus(StatusTemplate status) => new(status);

        public static StatusSourceInfo FromSkill(Skill skill, StatusTemplate status) {
            return status.alwaysShowSeparately 
                       ? new StatusSourceInfo(status).WithCharacter(skill.Owner)
                       : new StatusSourceInfo(skill, status);
        }

        public static StatusSourceInfo FromItemBuff(AppliedItemBuff itemBuff, ItemTemplate buffTemplate) {
            var ssi = new StatusSourceInfo();
            ssi.SourceItem = itemBuff.Item;
            ssi.SourceUniqueID = itemBuff.Item?.ID ?? NewGUID();
            ssi.SourceCharacter = new WeakModelRef<ICharacter>(itemBuff.Character);
            ssi.DisplayNameLoc = buffTemplate.itemName;
            ssi.SourceDescriptionLoc = buffTemplate.DescriptionLoc;
            ssi.Icon = buffTemplate.IconReference();
            ssi.HiddenOnUI = false;
            ssi.AlwaysShowSeparately = true;
            return ssi;
        }

        public StatusSourceInfo WithCharacter(ICharacter character) {
            SourceCharacter = new WeakModelRef<ICharacter>(character);
            return this;
        }
        
        public StatusSourceInfo WithItem(Item item) {
            SourceItem = new WeakModelRef<Item>(item);
            return this;
        }
        
        static string NewGUID() => Guid.NewGuid().ToString();
    }
}
