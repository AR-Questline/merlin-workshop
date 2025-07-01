using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Skills.Passives {
    public partial class RecursivePassiveEffects : Element<Skill>, IPassiveEffect, ISkillOwner {
        public sealed override bool IsNotSaved => true;

        List<SkillReference> _skillReferences;

        public Skill Skill => ParentModel;
        public ICharacter Character => Skill.Owner;

        public RecursivePassiveEffects(List<SkillReference> skillReferences) {
            _skillReferences = skillReferences;
        }

        protected override void OnInitialize() {
            foreach (var skill in _skillReferences.Select(r => r.CreateSkill())) {
                AddElement(skill);
                skill.Learn();
                Skill.ListenTo(Skill.Events.ContextChanged, () => skill.Trigger(Skill.Events.ContextChanged, skill), this);
            }
        }
    }
}