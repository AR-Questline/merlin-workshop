using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills;

namespace Awaken.TG.Main.Utility.Skills {
    public static class SkillReferenceUtils {
        public static string Description(ICharacter owner, SkillReference skillRef) {
            DummySkillCharacter dummySkillOwner = DummySkillCharacter.GetOrCreateInstance;
            Skill skill = skillRef.CreateSkill();
            dummySkillOwner.AddElement(skill);
            string description = skill.DescriptionFor(owner);
            skill.Discard();
            return description;
        }

        public static float? GetVariable(string name, ICharacter owner, SkillReference skillRef) {
            if (skillRef == null) return null;
            if (string.IsNullOrWhiteSpace(name)) return null;
            
            DummySkillCharacter dummySkillOwner = DummySkillCharacter.GetOrCreateInstance;
            Skill skill = skillRef.CreateSkill();
            dummySkillOwner.AddElement(skill);
            float? variable = skill.GetVariable(name, owner);
            skill.Discard();
            return variable;
        }
        
        public static StatType GetEnumVariable(string name, SkillReference skillRef) {
            if (skillRef == null) return null;
            if (string.IsNullOrWhiteSpace(name)) return null;
            
            DummySkillCharacter dummySkillOwner = DummySkillCharacter.GetOrCreateInstance;
            Skill skill = skillRef.CreateSkill();
            dummySkillOwner.AddElement(skill);
            StatType statType = skill.GetRichEnum(name);
            skill.Discard();
            return statType;
        }
    }
}