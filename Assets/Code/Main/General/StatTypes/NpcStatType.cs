using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.General.StatTypes {
    [RichEnumDisplayCategory("Npc")]
    public class NpcStatType : StatType<NpcElement> {

        public static readonly NpcStatType
            Sight = new(nameof(Sight), "", npc => npc.NpcStats.Sight, "Perception"),
            SightLengthMultiplier = new(nameof(SightLengthMultiplier), "", npc => npc.NpcStats.SightLengthMultiplier, "Perception"),
            Hearing = new(nameof(Hearing), "", npc => npc.NpcStats.Hearing, "Perception"),
            PoiseThreshold = new(nameof(PoiseThreshold), "", npc => npc.NpcStats.PoiseThreshold, "Combat"),
            ForceStumbleThreshold = new(nameof(ForceStumbleThreshold), "", npc => npc.NpcStats.ForceStumbleThreshold, "Combat"),
            Block = new(nameof(Block), "", npc => npc.NpcStats.Block, "Combat"),
            BlockPenaltyMultiplier = new(nameof(BlockPenaltyMultiplier), "", npc => npc.NpcStats.BlockPenaltyMultiplier, "Combat"),
            MeleeDamage = new(nameof(MeleeDamage), "", npc => npc.NpcStats.MeleeDamage, "Combat"),
            RangedDamage = new(nameof(RangedDamage), "", npc => npc.NpcStats.RangedDamage, "Combat"),
            MagicDamage = new(nameof(MagicDamage), "", npc => npc.NpcStats.MagicDamage, "Combat"),
            ForceDamageMultiplier = new(nameof(ForceDamageMultiplier), "", npc => npc.NpcStats.ForceDamageMultiplier, "Combat"),
            HeroKnockBack = new(nameof(HeroKnockBack), "", npc => npc.NpcStats.HeroKnockBack, "Combat");
        
        protected NpcStatType(string id, string displayName, Func<NpcElement, Stat> getter, string inspectorCategory = "", Param param = null) : base(id, displayName, getter, inspectorCategory, param) { }
    }
}