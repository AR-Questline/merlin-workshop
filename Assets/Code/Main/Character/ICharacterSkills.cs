using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Character {
    public interface ICharacterSkills : IModel {
        [UnityEngine.Scripting.Preserve] ICharacter Character { get; }
        void UpdateContext();
        IEnumerable<Skill> AllSkills();
        void RefreshSkillState();
        // --- Skills
        [UnityEngine.Scripting.Preserve] Skill LearnSkill(SkillGraph graph);
        Skill LearnSkill(Skill skill);
    }
}