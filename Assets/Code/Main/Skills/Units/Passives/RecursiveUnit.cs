using System.Collections.Generic;
using Awaken.TG.Main.Skills.Passives;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RecursiveUnit : PassiveSpawnerUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public List<SkillReference> skills;

        protected override void Definition() { }

        protected override IPassiveEffect Passive(Skill skill, Flow flow) {
            return new RecursivePassiveEffects(skills);
        }
    }
}