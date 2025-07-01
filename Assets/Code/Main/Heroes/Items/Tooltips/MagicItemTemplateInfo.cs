using System;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Enums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    [Serializable, InlineProperty]
    public class MagicItemTemplateInfo {
        [SerializeField, RichEnumExtends(typeof(MagicType))] 
        RichEnumReference magicType = MagicType.None;
        
        [SerializeField, RichEnumExtends(typeof(MagicEffectType))] 
        RichEnumReference effectType = MagicEffectType.Damage;
        [SerializeField, Toggle(nameof(OptionalLocString.toggled))]
        OptionalLocString effectOverridenToken;
        
        [SerializeField, RichEnumExtends(typeof(StatType))] 
        RichEnumReference costType = CharacterStatType.Mana;
        [SerializeField, Toggle(nameof(OptionalLocString.toggled))]
        OptionalLocString costOverridenToken;
        
        [SerializeField, Toggle(nameof(OptionalLocString.toggled))] 
        OptionalLocString magicDescription;

        public bool IsActive => magicDescription.toggled;
        public MagicType MagicType => magicType.EnumAs<MagicType>() ?? MagicType.None;
        public string MagicDescription => magicDescription.LocString;
        public MagicEffectType EffectType => effectType.EnumAs<MagicEffectType>() ?? MagicEffectType.Damage;
        public bool IsEffectOverriden => effectOverridenToken.toggled;
        public string EffectOverridenToken => effectOverridenToken.LocString;
        public StatType CostType => costType.EnumAs<StatType>() ?? CharacterStatType.Mana;
        public bool IsCostOverriden => costOverridenToken.toggled;
        public string CostOverridenToken => costOverridenToken.LocString;
    }

    public class MagicEffectType : RichEnum {
        public LocString DisplayName { [UnityEngine.Scripting.Preserve] get; }

        public static readonly MagicEffectType
            None = new(nameof(None), LocTerms.None),
            Damage = new(nameof(Damage), LocTerms.BaseDamage),
            Health = new(nameof(Health), LocTerms.Health);
        
        MagicEffectType(string enumName, string displayName) : base(enumName) {
            DisplayName = new LocString {ID = displayName};
        }
    }

    public class MagicType : RichEnum {
        public LocString DisplayName { get; }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly MagicType
            None = new(nameof(None), LocTerms.None),
            AoE = new(nameof(AoE), LocTerms.MagicTypeAoE),
            Buff = new(nameof(Buff), LocTerms.MagicTypeBuff),
            Projectile = new(nameof(Projectile), LocTerms.MagicTypeProjectile),
            Summon = new(nameof(Summon), LocTerms.MagicTypeSummon),
            Trap = new(nameof(Trap), LocTerms.MagicTypeTrap),
            Ray = new(nameof(Ray), LocTerms.MagicTypeRay),
            Channeled = new(nameof(Channeled), LocTerms.MagicTypeChanneled);
        
        MagicType(string enumName, string displayName) : base(enumName) {
            DisplayName = new LocString {ID = displayName};
        }
    } 
}
