using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Main.Utility.TokenTexts;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public struct TalentVariablesRetriever : ITextVariablesContainer {
        Talent _talent;
        int _level;
            
        public TalentVariablesRetriever(Talent talent, int level) {
            _talent = talent;
            _level = level;
        }
            
        public float? GetVariable(string id, int index = 0, ICharacter owner = null) {
            return SkillReferenceUtils.GetVariable(id, owner, RetrieveSkillRefForVariable(id, index));
        }

        public StatType GetEnum(string id, int index = 0) {
            return SkillReferenceUtils.GetEnumVariable(id, RetrieveSkillRefForEnum(id, index));
        }
            
        SkillReference RetrieveSkillRefForVariable(string id, int index) {
            if (index == 0) {
                return _talent.Template.GetLevel(_level).Skills.FirstOrDefault(s => s.variables.Any(v => v.name == id));
            }

            var skills = _talent.Template.GetLevel(_level).Skills;
            return index < skills.Count ? skills[index] : null;
        }

        SkillReference RetrieveSkillRefForEnum(string id, int index) {
            if (index == 0) {
                return _talent.Template.GetLevel(_level).Skills.FirstOrDefault(s => s.enums.Any(v => v.name == id));
            }
            
            var skills = _talent.Template.GetLevel(_level).Skills;
            return index < skills.Count ? skills[index] : null;
        }
    }
}