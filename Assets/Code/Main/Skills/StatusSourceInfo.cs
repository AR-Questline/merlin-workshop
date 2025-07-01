using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Statuses;
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
        [Saved] public UnicodeString DisplayName { get; private set; }
        [Saved] public UnicodeString SourceDescription { get; private set; }
        [Saved] public ShareableSpriteReference Icon { get; private set; }
        [Saved(false)] public bool HiddenOnUI { get; private set; }
        [Saved(false)] public bool AlwaysShowSeparately { get; private set; }

        public string DisplayNameString => DisplayName;
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public ICharacter GetSourceCharacter => SourceCharacter.Get();

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public StatusSourceInfo() { }
        
        StatusSourceInfo(StatusTemplate statusTemplate) {
            AlwaysShowSeparately = statusTemplate.alwaysShowSeparately;
            SourceUniqueID = NewGUID();
            
            DisplayName = statusTemplate.displayName?.ToString();
            SourceDescription = statusTemplate.description?.ToString();
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
            DisplayName = skill.DisplayName;
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
            DisplayName = source.DisplayName;
            SourceDescription = source.SourceDescription;
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
            ssi.DisplayName = buffTemplate.ItemName;
            ssi.SourceDescription = buffTemplate.Description;
            ssi.Icon = buffTemplate.IconReference;
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
