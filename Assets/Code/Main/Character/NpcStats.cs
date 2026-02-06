using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Character {
    public sealed partial class NpcStats : Element<NpcElement> {
        public override ushort TypeForSerialization => SavedModels.NpcStats;

        [Saved] NpcStatsWrapper _wrapper;
        
        public LimitedStat Sight { get; private set; }
        public LimitedStat SightLengthMultiplier { get; private set; }
        public LimitedStat Hearing { get; private set; }
        public LimitedStat PoiseThreshold { get; private set; }
        public LimitedStat ForceStumbleThreshold { get; private set; }
        public LimitedStat Block { get; private set; }
        public LimitedStat BlockPenaltyMultiplier { get; private set; }
        public LimitedStat MeleeDamage { get; private set; }
        public LimitedStat RangedDamage { get; private set; }
        public LimitedStat MagicDamage { get; private set; }
        public LimitedStat ForceDamageMultiplier { get; private set; }
        public Stat HeroKnockBack { get; private set; }
        public Stat BackToSpawnPointDistanceMultiplier { get; private set; }
        
        protected override void OnInitialize() {
            _wrapper.Initialize(this, ParentModel.HeroLevelAtInitialization);
        }

        public static NpcStats CreateFromNpcTemplate(NpcElement npc) {
            var stats = npc.AddElement(new NpcStats());
            return stats;
        }
        
        public void RecalculateAllStats(int previousHereLevel, int newHeroLevel) {
            _wrapper.PrepareForSave(this, previousHereLevel);
            _wrapper.Initialize(this, newHeroLevel);
        }
        
        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this, ParentModel.HeroLevelAtInitialization);
        }
        
        public partial struct NpcStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.NpcStatsWrapper;

            const float DefaultPerceptionValues = 1f;
            const float DefaultPoiseValue = 0f;
            
            [Saved(0f)] float SightDif;
            [Saved(0f)] float SightLengthMultiplierDif;
            [Saved(0f)] float HearingDif;
            [Saved(0f)] float PoiseThresholdDif;
            [Saved(0f)] float ForceStumbleThresholdDif;
            [Saved(0f)] float BlockDif;
            [Saved(0f)] float BlockPenaltyMultiplierDif;
            [Saved(0f)] float MeleeDamageDif;
            [Saved(0f)] float RangedDamageDif;
            [Saved(0f)] float MagicDamageDif;
            [Saved(0f)] float ForceDamageMultiplierDif;
            [Saved(0f)] float HeroKnockBackDif;
            [Saved(0f)] float BackToSpawnPointDistanceMultiplierDif;

            public void Initialize(NpcStats stats, int heroLevel) {
                NpcElement npc = stats.ParentModel;
                NpcTemplate template = npc.Template;
                
                int ngLevel = npc.NewGamePlusLevel;
                NewGamePlusSystem.GetAllNpcStats(npc, template, ngLevel, heroLevel, out var meleeDamage, out var rangedDamage, out var magicDamage, out var poiseThreshold, out var forceStumbleThreshold);
                
                stats.Sight = new LimitedStat(npc, NpcStatType.Sight, DefaultPerceptionValues + SightDif, 0, 1);
                stats.SightLengthMultiplier = new LimitedStat(npc, NpcStatType.SightLengthMultiplier, DefaultPerceptionValues + SightLengthMultiplierDif, 0, 2);
                stats.Hearing = new LimitedStat(npc, NpcStatType.Hearing, DefaultPerceptionValues + HearingDif, 0, 1);
                stats.PoiseThreshold = new LimitedStat(npc, NpcStatType.PoiseThreshold, DefaultPoiseValue + PoiseThresholdDif, 0, poiseThreshold, true);
                stats.ForceStumbleThreshold = new LimitedStat(npc, NpcStatType.ForceStumbleThreshold, DefaultPoiseValue + ForceStumbleThresholdDif, 0, forceStumbleThreshold, true);
                
                stats.Block = new LimitedStat(npc, NpcStatType.Block, template.blockValue + BlockDif, 0, 100);
                stats.BlockPenaltyMultiplier = new LimitedStat(npc, NpcStatType.BlockPenaltyMultiplier, template.blockPenaltyMultiplier + BlockPenaltyMultiplierDif, 0, 2);
                stats.MeleeDamage = new LimitedStat(npc, NpcStatType.MeleeDamage, meleeDamage + MeleeDamageDif, 1, float.MaxValue);
                stats.RangedDamage = new LimitedStat(npc, NpcStatType.RangedDamage, rangedDamage + RangedDamageDif, 1, float.MaxValue);
                stats.MagicDamage = new LimitedStat(npc, NpcStatType.MagicDamage, magicDamage + MagicDamageDif, 1, float.MaxValue);
                stats.ForceDamageMultiplier = new LimitedStat(npc, NpcStatType.ForceDamageMultiplier, 1 + ForceDamageMultiplierDif, 0, float.MaxValue);
                stats.HeroKnockBack = new Stat(npc, NpcStatType.HeroKnockBack, template.heroKnockBack + HeroKnockBackDif);
                stats.BackToSpawnPointDistanceMultiplier = new Stat(npc, NpcStatType.BackToSpawnPointDistanceMultiplier, template.BackToSpawnPointDistanceMultiplier + BackToSpawnPointDistanceMultiplierDif);
            }

            public void PrepareForSave(NpcStats npcStats, int heroLevel) {
                NpcElement npc = npcStats.ParentModel;
                NpcTemplate template = npc.Template;
                
                int ngLevel = npc.NewGamePlusLevel;
                NewGamePlusSystem.GetAllNpcStats(npc, template, ngLevel, heroLevel, out var meleeDamage, out var rangedDamage, out var magicDamage, out _, out _);
                
                SightDif = npcStats.Sight.ValueForSave - DefaultPerceptionValues;
                SightLengthMultiplierDif = npcStats.SightLengthMultiplier.ValueForSave - DefaultPerceptionValues;
                HearingDif = npcStats.Hearing.ValueForSave - DefaultPerceptionValues;
                PoiseThresholdDif = npcStats.PoiseThreshold.ValueForSave - DefaultPoiseValue;
                ForceStumbleThresholdDif = npcStats.ForceStumbleThreshold.ValueForSave - DefaultPoiseValue;
                
                BlockDif = npcStats.Block.ValueForSave - template.blockValue;
                BlockPenaltyMultiplierDif = npcStats.BlockPenaltyMultiplier.ValueForSave - template.blockPenaltyMultiplier;
                MeleeDamageDif = npcStats.MeleeDamage.ValueForSave - meleeDamage;
                RangedDamageDif = npcStats.RangedDamage.ValueForSave - rangedDamage;
                MagicDamageDif = npcStats.MagicDamage.ValueForSave - magicDamage;
                ForceDamageMultiplierDif = npcStats.ForceDamageMultiplier.ValueForSave - 1;
                HeroKnockBackDif = npcStats.HeroKnockBack.ValueForSave - template.heroKnockBack;
                BackToSpawnPointDistanceMultiplierDif = npcStats.BackToSpawnPointDistanceMultiplier.ValueForSave - template.BackToSpawnPointDistanceMultiplier;
            }
        }
    }
}