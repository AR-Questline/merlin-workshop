using UnityEngine;
using Unity.VisualScripting;
using Awaken.TG.Main.Heroes.Combat;

namespace Awaken.TG.VisualScripts.Units.Generated.AI_Systems.General {
    [UnitCategory("Generated/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetPlayer : ARGeneratedUnit {

        ControlInput enter;
        ControlOutput exit;
        
        
        
        ValueOutput CombatTarget;
        
        protected override void Definition() {
            enter = ControlInput("Enter", Enter);
            exit = ControlOutput("Exit");
            Succession(enter, exit);

            

            CombatTarget = ValueOutput<Transform>("CombatTarget");
        }

        ControlOutput Enter(Flow flow) {
            
            
            Invoke(flow.stack.gameObject, flow, out Transform _CombatTarget);
            
            flow.SetValue(CombatTarget, _CombatTarget);
            
            return exit;
        }
        
        public static void Invoke(GameObject gameObject, Flow flow, out Transform CombatTarget) {
			object value = (object)Variables.Application.Get("Player");
			CharacterController Output = (CharacterController) value;
			bool equal = Output == null;
			if (equal) {
				GameObject result = GameObject.FindWithTag("Player");
				if (result != null) {
					Component result_1 = result.GetComponent(typeof(CharacterController));
					Variables.Application.Set("Player", result_1);
					Component result_2 = result.GetComponent(typeof(VHeroController));
					Variables.Application.Set("VHeroController", result_2);
				}
			}
			object value_1 = (object)Variables.Application.Get("Player");
			if (value_1 != null) {
				CharacterController Output_1 = (CharacterController) value_1;
				Transform value_2 = Output_1.transform;
				flow.variables.Set("returnValue", value_2);
			} else {
				flow.variables.Set("returnValue", null);
			}
			Transform value_3 = (Transform)flow.variables.Get("returnValue");
			CombatTarget = value_3;
		}
    }
}