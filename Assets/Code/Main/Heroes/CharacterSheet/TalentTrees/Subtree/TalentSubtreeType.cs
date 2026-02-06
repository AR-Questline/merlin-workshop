using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Subtree {
    public class TalentSubtreeType : RichEnum {
        string NameKey { get; }

        public string DisplayName => NameKey.Translate();

        [UnityEngine.Scripting.Preserve] public static readonly TalentSubtreeType
            None = new(nameof(None), LocTerms.None),
            DexterityParry = new(nameof(DexterityParry), LocTerms.SkillTreeParry),
            DexterityAttackSpeed = new(nameof(DexterityAttackSpeed), LocTerms.SkillTreeAttackSpeed),
            DexterityMovement = new(nameof(DexterityMovement), LocTerms.SkillTreeMovement),
            DexterityBows = new(nameof(DexterityBows), LocTerms.SkillTreeBows),
            EnduranceShields = new(nameof(EnduranceShields), LocTerms.SkillTreeShields),
            EnduranceStamina = new(nameof(EnduranceStamina), LocTerms.SkillTreeStamina),
            EnduranceHealth = new(nameof(EnduranceHealth), LocTerms.SkillTreeHealth),
            PerceptionStealth = new(nameof(PerceptionStealth), LocTerms.SkillTreeStealth),
            PerceptionDaggers = new(nameof(PerceptionDaggers), LocTerms.SkillTreeDaggers),
            PerceptionCriticalHits = new(nameof(PerceptionCriticalHits), LocTerms.SkillTreeCriticalHits),
            PracticalityHealing = new(nameof(PracticalityHealing), LocTerms.SkillTreeHealing),
            PracticalityCrafting = new(nameof(PracticalityCrafting), LocTerms.SkillTreeCrafting),
            PracticalityArmor = new(nameof(PracticalityArmor), LocTerms.SkillTreeArmor),
            PracticalityStatuses = new(nameof(PracticalityStatuses), LocTerms.SkillTreeStatuses),
            RedDeathCombat = new(nameof(RedDeathCombat), LocTerms.RedDeathCombat),
            RedDeathSurvival = new(nameof(RedDeathSurvival), LocTerms.RedDeathSurvival),
            SpiritualityWands = new(nameof(SpiritualityWands), LocTerms.SkillTreeWands),
            SpiritualitySummoning = new(nameof(SpiritualitySummoning), LocTerms.SkillTreeSummoning),
            SpiritualityCombat = new(nameof(SpiritualityCombat), LocTerms.SkillTreeCombat),
            SpiritualityGeneralAndBuffs = new(nameof(SpiritualityGeneralAndBuffs), LocTerms.SkillTreeGeneralAndBuffs),
            StrengthOneHanded = new(nameof(StrengthOneHanded), LocTerms.SkillTreeOneHanded),
            StrengthTwoHanded = new(nameof(StrengthTwoHanded), LocTerms.SkillTreeTwoHanded),
            StrengthGeneral = new(nameof(StrengthGeneral), LocTerms.SkillTreeGeneral),
            StrengthUnarmed = new(nameof(StrengthUnarmed), LocTerms.SkillTreeUnarmed),
            KingPowerSoul = new(nameof(KingPowerSoul), string.Empty),
            KingPowerExcalibur = new(nameof(KingPowerExcalibur), string.Empty),
            KingPowerShield = new(nameof(KingPowerShield), string.Empty),
            KingPowerHelmet = new(nameof(KingPowerHelmet), string.Empty),
            SarrasWarrior = new(nameof(SarrasWarrior), LocTerms.SkillTreeSarrasWarrior),
            SarrasMage = new(nameof(SarrasMage), LocTerms.SkillTreeSarrasMage),
            SarrasRogue = new(nameof(SarrasRogue), LocTerms.SkillTreeSarrasRogue);

        TalentSubtreeType(string enumName, string nameKey) : base(enumName) {
            NameKey = nameKey;
        }

        public TalentTreeBranchType ToSarrasTreeBranchType() {
            if (this == SarrasWarrior) return TalentTreeBranchType.SarrasWarrior;
            if (this == SarrasMage) return TalentTreeBranchType.SarrasMage;
            if (this == SarrasRogue) return TalentTreeBranchType.SarrasRogue;
            return TalentTreeBranchType.None;
        }
    }
}
