using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Character {
    /// <summary>
    /// Stats used for build up statuses.
    /// </summary>
    public sealed partial class StatusStats : Element<ICharacter> {
        public override ushort TypeForSerialization => SavedModels.StatusStats;

        const float MaxLimitStatMax = 999999f;

        [Saved] StatusStatsWrapper _wrapper;
        
        //statuses
        public LimitedStat BleedBuildup { get; private set; }
        public LimitedStat BleedEffectModifier { get; private set; }
        public LimitedStat BurnBuildup { get; private set; }
        public LimitedStat BurnEffectModifier { get; private set; }
        public LimitedStat FrenzyBuildup { get; private set; }
        public LimitedStat FrenzyEffectModifier { get; private set; }
        public LimitedStat ConfusionBuildup { get; private set; }
        public LimitedStat ConfusionEffectModifier { get; private set; }
        public LimitedStat CorruptionBuildup { get; private set; }
        public LimitedStat CorruptionEffectModifier { get; private set; }
        public LimitedStat MuteBuildup { get; private set; }
        public LimitedStat MuteEffectModifier { get; private set; }
        public LimitedStat PoisonBuildup { get; private set; }
        public LimitedStat PoisonEffectModifier { get; private set; }
        public LimitedStat SlowBuildup { get; private set; }
        public LimitedStat SlowEffectModifier { get; private set; }
        public LimitedStat StunBuildup { get; private set; }
        public LimitedStat StunEffectModifier { get; private set; }
        public LimitedStat WeakBuildup { get; private set; }
        public LimitedStat WeakEffectModifier { get; private set; }
        public LimitedStat DrunkBuildup { get; private set; }
        public LimitedStat DrunkEffectModifier { get; private set; }
        public LimitedStat IntoxicatedBuildup { get; private set; }
        public LimitedStat IntoxicatedEffectModifier { get; private set; }
        public LimitedStat FullBuildup { get; private set; }
        public LimitedStat FullEffectModifier { get; private set; }
        public TemplateReference[] InvulnerableToStatuses { get; private set; }

        // === Events

        protected override void OnInitialize() {
            _wrapper.Initialize(this);
        }
        
        public static void Create(ICharacter character) {
            StatusStats stats = new();
            character.AddElement(stats);
        }

        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public interface ITemplate {
            public ref StatusStatsValues StatusStats { get; }
        }
        
        public partial struct StatusStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.StatusStatsWrapper;

            [Saved(0f)] float BleedBuildupDif;
            [Saved(0f)] float BleedEffectModifierDif;
            [Saved(0f)] float BurnBuildupDif;
            [Saved(0f)] float BurnEffectModifierDif;
            [Saved(0f)] float FrenzyBuildupDif;
            [Saved(0f)] float FrenzyEffectModifierDif;
            [Saved(0f)] float ConfusionBuildupDif;
            [Saved(0f)] float ConfusionEffectModifierDif;
            [Saved(0f)] float CorruptionBuildupDif;
            [Saved(0f)] float CorruptionEffectModifierDif;
            [Saved(0f)] float MuteBuildupDif;
            [Saved(0f)] float MuteEffectModifierDif;
            [Saved(0f)] float PoisonBuildupDif;
            [Saved(0f)] float PoisonEffectModifierDif;
            [Saved(0f)] float SlowBuildupDif;
            [Saved(0f)] float SlowEffectModifierDif;
            [Saved(0f)] float StunBuildupDif;
            [Saved(0f)] float StunEffectModifierDif;
            [Saved(0f)] float WeakBuildupDif;
            [Saved(0f)] float WeakEffectModifierDif;
            [Saved(0f)] float DrunkBuildupDif;
            [Saved(0f)] float DrunkEffectModifierDif;
            [Saved(0f)] float IntoxicatedBuildupDif;
            [Saved(0f)] float IntoxicatedEffectModifierDif;
            [Saved(0f)] float FullBuildupDif;
            [Saved(0f)] float FullEffectModifierDif;

            public void Initialize(StatusStats stats) {
                ICharacter owner = stats.ParentModel;
                StatusStatsValues statValues = owner.StatusStatsTemplate.StatusStats;
                int tier = owner.Tier;
                
                stats.BleedBuildup = new LimitedStat(owner, StatusStatType.BleedBuildup, statValues.Bleed.GetThreshold(tier) + BleedBuildupDif, 1, MaxLimitStatMax);
                stats.BleedEffectModifier = new LimitedStat(owner, StatusStatType.BleedEffectModifier, statValues.Bleed.GetModifier() + BleedEffectModifierDif, 0, MaxLimitStatMax);
                stats.BurnBuildup = new LimitedStat(owner, StatusStatType.BurnBuildup, statValues.Burn.GetThreshold(tier) + BurnBuildupDif, 1, MaxLimitStatMax);
                stats.BurnEffectModifier = new LimitedStat(owner, StatusStatType.BurnEffectModifier, statValues.Burn.GetModifier() + BurnEffectModifierDif, 0, MaxLimitStatMax);
                stats.FrenzyBuildup = new LimitedStat(owner, StatusStatType.FrenzyBuildup, statValues.Frenzy.GetThreshold(tier) + FrenzyBuildupDif, 1, MaxLimitStatMax);
                stats.FrenzyEffectModifier = new LimitedStat(owner, StatusStatType.FrenzyEffectModifier, statValues.Frenzy.GetModifier() + FrenzyEffectModifierDif, 0, MaxLimitStatMax);
                stats.ConfusionBuildup = new LimitedStat(owner, StatusStatType.ConfusionBuildup, statValues.Confusion.GetThreshold(tier) + ConfusionBuildupDif, 1, MaxLimitStatMax);
                stats.ConfusionEffectModifier = new LimitedStat(owner, StatusStatType.ConfusionEffectModifier, statValues.Confusion.GetModifier() + ConfusionEffectModifierDif, 0, MaxLimitStatMax);
                stats.CorruptionBuildup = new LimitedStat(owner, StatusStatType.CorruptionBuildup, statValues.Corruption.GetThreshold(tier) + CorruptionBuildupDif, 1, MaxLimitStatMax);
                stats.CorruptionEffectModifier = new LimitedStat(owner, StatusStatType.CorruptionEffectModifier, statValues.Corruption.GetModifier() + CorruptionEffectModifierDif, 0, MaxLimitStatMax);
                stats.MuteBuildup = new LimitedStat(owner, StatusStatType.MuteBuildup, statValues.Mute.GetThreshold(tier) + MuteBuildupDif, 1, MaxLimitStatMax);
                stats.MuteEffectModifier = new LimitedStat(owner, StatusStatType.MuteEffectModifier, statValues.Mute.GetModifier() + MuteEffectModifierDif, 0, MaxLimitStatMax);
                stats.PoisonBuildup = new LimitedStat(owner, StatusStatType.PoisonBuildup, statValues.Poison.GetThreshold(tier) + PoisonBuildupDif, 1, MaxLimitStatMax);
                stats.PoisonEffectModifier = new LimitedStat(owner, StatusStatType.PoisonEffectModifier, statValues.Poison.GetModifier() + PoisonEffectModifierDif, 0, MaxLimitStatMax);
                stats.SlowBuildup = new LimitedStat(owner, StatusStatType.SlowBuildup, statValues.Slow.GetThreshold(tier) + SlowBuildupDif, 1, MaxLimitStatMax);
                stats.SlowEffectModifier = new LimitedStat(owner, StatusStatType.SlowEffectModifier, statValues.Slow.GetModifier() + SlowEffectModifierDif, 0, MaxLimitStatMax);
                stats.StunBuildup = new LimitedStat(owner, StatusStatType.StunBuildup, statValues.Stun.GetThreshold(tier) + StunBuildupDif, 1, MaxLimitStatMax);
                stats.StunEffectModifier = new LimitedStat(owner, StatusStatType.StunEffectModifier, statValues.Stun.GetModifier() + StunEffectModifierDif, 0, MaxLimitStatMax);
                stats.WeakBuildup = new LimitedStat(owner, StatusStatType.WeakBuildup, statValues.Weak.GetThreshold(tier) + WeakBuildupDif, 1, MaxLimitStatMax);
                stats.WeakEffectModifier = new LimitedStat(owner, StatusStatType.WeakEffectModifier, statValues.Weak.GetModifier() + WeakEffectModifierDif, 0, MaxLimitStatMax);
                
                stats.DrunkBuildup = new LimitedStat(owner, StatusStatType.DrunkBuildup, statValues.Drunk.GetThreshold(tier) + DrunkBuildupDif, 1, MaxLimitStatMax);
                stats.DrunkEffectModifier = new LimitedStat(owner, StatusStatType.DrunkEffectModifier, statValues.Drunk.GetModifier() + DrunkEffectModifierDif, 0, MaxLimitStatMax);
                stats.IntoxicatedBuildup = new LimitedStat(owner, StatusStatType.IntoxicatedBuildup, statValues.Intoxicated.GetThreshold(tier) + IntoxicatedBuildupDif, 1, MaxLimitStatMax);
                stats.IntoxicatedEffectModifier = new LimitedStat(owner, StatusStatType.IntoxicatedEffectModifier, statValues.Intoxicated.GetModifier() + IntoxicatedEffectModifierDif, 0, MaxLimitStatMax);
                stats.FullBuildup = new LimitedStat(owner, StatusStatType.FullBuildup, statValues.Full.GetThreshold(tier) + FullBuildupDif, 1, MaxLimitStatMax);
                stats.FullEffectModifier = new LimitedStat(owner, StatusStatType.FullEffectModifier, statValues.Full.GetModifier() + FullEffectModifierDif, 0, MaxLimitStatMax);

                stats.InvulnerableToStatuses = statValues.InvulnerableToStatuses;
            }

            public void PrepareForSave(StatusStats statusStats) {
                StatusStatsValues statValues = statusStats.ParentModel.StatusStatsTemplate.StatusStats;
                int tier = statusStats.ParentModel.Tier;
                
                BleedBuildupDif = statusStats.BleedBuildup.BaseValue - statValues.Bleed.GetThreshold(tier);
                BleedEffectModifierDif = statusStats.BleedEffectModifier.BaseValue - statValues.Bleed.GetModifier();
                BurnBuildupDif = statusStats.BurnBuildup.BaseValue - statValues.Burn.GetThreshold(tier);
                BurnEffectModifierDif = statusStats.BurnEffectModifier.BaseValue - statValues.Burn.GetModifier();
                FrenzyBuildupDif = statusStats.FrenzyBuildup.BaseValue - statValues.Frenzy.GetThreshold(tier);
                FrenzyEffectModifierDif = statusStats.FrenzyEffectModifier.BaseValue - statValues.Frenzy.GetModifier();
                ConfusionBuildupDif = statusStats.ConfusionBuildup.BaseValue - statValues.Confusion.GetThreshold(tier);
                ConfusionEffectModifierDif = statusStats.ConfusionEffectModifier.BaseValue - statValues.Confusion.GetModifier();
                CorruptionBuildupDif = statusStats.CorruptionBuildup.BaseValue - statValues.Corruption.GetThreshold(tier);
                CorruptionEffectModifierDif = statusStats.CorruptionEffectModifier.BaseValue - statValues.Corruption.GetModifier();
                MuteBuildupDif = statusStats.MuteBuildup.BaseValue - statValues.Mute.GetThreshold(tier);
                MuteEffectModifierDif = statusStats.MuteEffectModifier.BaseValue - statValues.Mute.GetModifier();
                PoisonBuildupDif = statusStats.PoisonBuildup.BaseValue - statValues.Poison.GetThreshold(tier);
                PoisonEffectModifierDif = statusStats.PoisonEffectModifier.BaseValue - statValues.Poison.GetModifier();
                SlowBuildupDif = statusStats.SlowBuildup.BaseValue - statValues.Slow.GetThreshold(tier);
                SlowEffectModifierDif = statusStats.SlowEffectModifier.BaseValue - statValues.Slow.GetModifier();
                StunBuildupDif = statusStats.StunBuildup.BaseValue - statValues.Stun.GetThreshold(tier);
                StunEffectModifierDif = statusStats.StunEffectModifier.BaseValue - statValues.Stun.GetModifier();
                WeakBuildupDif = statusStats.WeakBuildup.BaseValue - statValues.Weak.GetThreshold(tier);
                WeakEffectModifierDif = statusStats.WeakEffectModifier.BaseValue - statValues.Weak.GetModifier();
                
                DrunkBuildupDif = statusStats.DrunkBuildup.BaseValue - statValues.Drunk.GetThreshold(tier);
                DrunkEffectModifierDif = statusStats.DrunkEffectModifier.BaseValue - statValues.Drunk.GetModifier();
                IntoxicatedBuildupDif = statusStats.IntoxicatedBuildup.BaseValue - statValues.Intoxicated.GetThreshold(tier);
                IntoxicatedEffectModifierDif = statusStats.IntoxicatedEffectModifier.BaseValue - statValues.Intoxicated.GetModifier();
                FullBuildupDif = statusStats.FullBuildup.BaseValue - statValues.Full.GetThreshold(tier);
                FullEffectModifierDif = statusStats.FullEffectModifier.BaseValue - statValues.Full.GetModifier();
            }
        }
    }
}