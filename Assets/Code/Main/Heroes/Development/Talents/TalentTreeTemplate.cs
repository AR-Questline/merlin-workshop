using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public class TalentTreeTemplate : ScriptableObject, ITemplate {
        public string GUID { get; set; }
        
        [SerializeField, HideInInspector] TemplateMetadata metadata;
        public TemplateMetadata Metadata => metadata;
        
        string INamed.DisplayName => Name;
        string INamed.DebugName => name;
        
        [SerializeField, LocStringCategory(Category.UI)] LocString displayName;

        [SerializeField, RichEnumExtends(typeof(StatType))]
        RichEnumReference currencyStatType;
        [SerializeField, Required] 
        VTalentTreePatternBase pattern;
        [SerializeField, UIAssetReference(AddressableLabels.UI.Talents), ShowAssetPreview]
        ShareableSpriteReference icon;
        
        public ShareableSpriteReference Icon => icon;
        public string Name => displayName;
        public StatType CurrencyStatType => currencyStatType.EnumAs<StatType>();
        public VTalentTreePatternBase Pattern => pattern;
    }
}
