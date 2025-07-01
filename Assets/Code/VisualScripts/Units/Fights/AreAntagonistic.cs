using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    public class AreAntagonistic : ARGeneratedUnit {

        ValueInput _character0;
        ValueInput _character1;
        [UnityEngine.Scripting.Preserve] ValueOutput _antagonistic;
        
        protected override void Definition() {
            _character0 = ValueInput<Component>("character 0");
            _character1 = ValueInput<Component>("character 1");
            _antagonistic = ValueOutput<bool>("antagonistic", Trigger);
        }

        bool Trigger(Flow flow) {
            var character0 = flow.GetValue<Component>(_character0).GetComponentInParent<ICharacterView>().Character;
            var character1 = flow.GetValue<Component>(_character1).GetComponentInParent<ICharacterView>().Character;
            return character1.IsHostileTo(character0);
        }
    }
}