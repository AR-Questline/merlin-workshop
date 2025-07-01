using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("SkillVariables")]
    [UnityEngine.Scripting.Preserve]
    public class SkillVariableOverrideUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int variablesCount;
        [Serialize, Inspectable, UnitHeaderInspectable] public int enumsCount;

        InlineValueInput<string>[] _enumNames;
        RequiredValueInput<StatType>[] _enumValues;
        
        InlineValueInput<string>[] _variablesNames;
        InlineValueInput<float>[] _variablesValues;
        
        protected override void Definition() {
            _variablesNames = new InlineValueInput<string>[variablesCount];
            _variablesValues = new InlineValueInput<float>[variablesCount];
            for (int i = 0; i < variablesCount; i++) {
                _variablesNames[i] = InlineARValueInput($"variable {i} name", "");
                _variablesValues[i] = InlineARValueInput<float>($"variable {i} value", 0);
            }
            
            _enumNames = new InlineValueInput<string>[enumsCount];
            _enumValues = new RequiredValueInput<StatType>[enumsCount];
            for (int i = 0; i < enumsCount; i++) {
                _enumNames[i] = InlineARValueInput($"enum {i} name", "");
                _enumValues[i] = RequiredARValueInput<StatType>($"enum {i} value");
            }

            ValueOutput("variables", flow => new SkillVariablesOverride(Variables(flow), Enums(flow)));
        }

        SkillVariable[] Variables(Flow flow) {
            var result = new SkillVariable[variablesCount];
            for (int i = 0; i < variablesCount; i++) {
                result[i] = new SkillVariable(_variablesNames[i].Value(flow), _variablesValues[i].Value(flow));
            }
            return result;
        }
        
        SkillRichEnum[] Enums(Flow flow) {
            var result = new SkillRichEnum[enumsCount];
            for (int i = 0; i < enumsCount; i++) {
                result[i] = new SkillRichEnum(_enumNames[i].Value(flow), _enumValues[i].Value(flow));
            }
            return result;
        }
    }
}