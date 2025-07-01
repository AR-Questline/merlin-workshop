using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.Utility;
using Awaken.Utility.Enums;
using UnityEngine.Localization;

namespace Awaken.TG.Main.Heroes.Statuses {
    public class StatusType : RichEnum {
        public LocalizedString DisplayName { [UnityEngine.Scripting.Preserve] get; }
        public bool IsPositive { get; }

        [UnityEngine.Scripting.Preserve]
        public static readonly StatusType
            Sin = new StatusType(nameof(Sin), LocTerms.Sin, isPositive: false),
            Debuff = new StatusType(nameof(Debuff), LocTerms.Debuff, isPositive: false),
            Curse = new StatusType(nameof(Curse), LocTerms.Curse, isPositive: false),
            Blessing = new StatusType(nameof(Blessing), LocTerms.Blessing, isPositive: true),
            Buff = new StatusType(nameof(Buff), LocTerms.Buff, isPositive: true),
            Grace = new StatusType(nameof(Grace), LocTerms.Buff, isPositive: true),
            Technical = new StatusType(nameof(Technical), LocTerms.TechnicalStatus, isPositive: true);

        StatusType(string enumName, string displayName, bool isPositive) : base(enumName) {
            DisplayName = new LocalizedString {
                TableReference = LocalizationHelper.DefaultTable,
                TableEntryReference = displayName
            };
            IsPositive = isPositive;
        }
    }
    
    public class BuildupStatusType : RichEnum {
        public StatType BuildupStatType { get; }
        public StatType EffectModifierType { get; }
        
        public static readonly BuildupStatusType
            Bleed = new(nameof(Bleed), StatusStatType.BleedBuildup, StatusStatType.BleedEffectModifier),
            Burn = new(nameof(Burn), StatusStatType.BurnBuildup, StatusStatType.BurnEffectModifier),

            Frenzy = new(nameof(Frenzy), StatusStatType.FrenzyBuildup, StatusStatType.FrenzyEffectModifier),
            Confusion = new(nameof(Confusion), StatusStatType.ConfusionBuildup, StatusStatType.ConfusionEffectModifier),
            Corruption = new(nameof(Corruption), StatusStatType.CorruptionBuildup, StatusStatType.CorruptionEffectModifier),

            Mute = new(nameof(Mute), StatusStatType.MuteBuildup, StatusStatType.MuteEffectModifier),
            Poison = new(nameof(Poison), StatusStatType.PoisonBuildup, StatusStatType.PoisonEffectModifier),

            Slow = new(nameof(Slow), StatusStatType.SlowBuildup, StatusStatType.SlowEffectModifier),
            Stun = new(nameof(Stun), StatusStatType.StunBuildup, StatusStatType.StunEffectModifier),
            Weak = new(nameof(Weak), StatusStatType.WeakBuildup, StatusStatType.WeakEffectModifier),

            Drunk = new(nameof(Drunk), StatusStatType.DrunkBuildup, StatusStatType.DrunkEffectModifier),
            Intoxicated = new(nameof(Intoxicated), StatusStatType.IntoxicatedBuildup, StatusStatType.IntoxicatedEffectModifier),
            Full = new(nameof(Full), StatusStatType.FullBuildup, StatusStatType.FullEffectModifier);

        protected BuildupStatusType(string enumName, StatType buildupStatType, StatType effectModifierType) : base(enumName) {
            BuildupStatType = buildupStatType;
            EffectModifierType = effectModifierType;
        }
    }
}