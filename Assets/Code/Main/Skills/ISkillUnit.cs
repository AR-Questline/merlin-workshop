using Awaken.TG.Main.Utility.Skills;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills {
    public interface ISkillUnit : IUnit {
        
    }

    public static class SkillUnits {
        public static Skill Skill(this ISkillUnit unit, Flow flow) {
            return flow.stack.machine switch {
                ScriptMachineWithSkill withSkill => withSkill.Owner,
                _ => null,
            };
        }
    }
}