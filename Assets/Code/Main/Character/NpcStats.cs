using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

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
        
        protected override void OnInitialize() {
            _wrapper.Initialize(this);
        }

        public static NpcStats CreateFromNpcTemplate(NpcElement npc) {
            var stats = npc.AddElement(new NpcStats());
            return stats;
        }
        
        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
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

            public void Initialize(NpcStats stats) {
                NpcElement npc = stats.ParentModel;
                NpcTemplate template = npc.Template;
                
                stats.Sight = new LimitedStat(npc, NpcStatType.Sight, DefaultPerceptionValues + SightDif, 0, 1);
                stats.SightLengthMultiplier = new LimitedStat(npc, NpcStatType.SightLengthMultiplier, DefaultPerceptionValues + SightLengthMultiplierDif, 0, 2);
                stats.Hearing = new LimitedStat(npc, NpcStatType.Hearing, DefaultPerceptionValues + HearingDif, 0, 1);
                stats.PoiseThreshold = new LimitedStat(npc, NpcStatType.PoiseThreshold, DefaultPoiseValue + PoiseThresholdDif, 0, template.poiseThreshold, true);
                stats.ForceStumbleThreshold = new LimitedStat(npc, NpcStatType.ForceStumbleThreshold, DefaultPoiseValue + ForceStumbleThresholdDif, 0, template.ForceStumbleThreshold, true);
                
                stats.Block = new LimitedStat(npc, NpcStatType.Block, template.blockValue + BlockDif, 0, 100);
                stats.BlockPenaltyMultiplier = new LimitedStat(npc, NpcStatType.BlockPenaltyMultiplier, template.blockPenaltyMultiplier + BlockPenaltyMultiplierDif, 0, 2);
                stats.MeleeDamage = new LimitedStat(npc, NpcStatType.MeleeDamage, template.meleeDamage + MeleeDamageDif, 1, float.MaxValue);
                stats.RangedDamage = new LimitedStat(npc, NpcStatType.RangedDamage, template.rangedDamage + RangedDamageDif, 1, float.MaxValue);
                stats.MagicDamage = new LimitedStat(npc, NpcStatType.MagicDamage, template.magicDamage + MagicDamageDif, 1, float.MaxValue);
                stats.ForceDamageMultiplier = new LimitedStat(npc, NpcStatType.ForceDamageMultiplier, 1 + ForceDamageMultiplierDif, 0, float.MaxValue);
                stats.HeroKnockBack = new Stat(npc, NpcStatType.HeroKnockBack, template.heroKnockBack);
            }

            public void PrepareForSave(NpcStats npcStats) {
                NpcTemplate template = npcStats.ParentModel.Template;
                
                SightDif = npcStats.Sight.ValueForSave - DefaultPerceptionValues;
                SightLengthMultiplierDif = npcStats.SightLengthMultiplier.ValueForSave - DefaultPerceptionValues;
                HearingDif = npcStats.Hearing.ValueForSave - DefaultPerceptionValues;
                PoiseThresholdDif = npcStats.PoiseThreshold.ValueForSave - DefaultPoiseValue;
                ForceStumbleThresholdDif = npcStats.ForceStumbleThreshold.ValueForSave - DefaultPoiseValue;
                
                BlockDif = npcStats.Block.ValueForSave - template.blockValue;
                BlockPenaltyMultiplierDif = npcStats.BlockPenaltyMultiplier.ValueForSave - template.blockPenaltyMultiplier;
                MeleeDamageDif = npcStats.MeleeDamage.ValueForSave - template.meleeDamage;
                RangedDamageDif = npcStats.RangedDamage.ValueForSave - template.rangedDamage;
                MagicDamageDif = npcStats.MagicDamage.ValueForSave - template.magicDamage;
                ForceDamageMultiplierDif = npcStats.ForceDamageMultiplier.ValueForSave - 1;
                HeroKnockBackDif = npcStats.HeroKnockBack.ValueForSave - template.heroKnockBack;
            }
        }
    }
}