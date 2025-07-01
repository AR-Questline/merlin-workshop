using System;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Skill Computable")]
    public class SkillComputableUnit : ARUnit, ISkillUnit, IGraphEventListener {
        public override bool isControlRoot => true;
        
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;

        InlineValueInput<float> _value;

        protected override void Definition() {
            _value = InlineARValueInput("Value", 0f);
        }

        public void StartListening(GraphStack stack) {
            var reference = stack.AsReference();
            using var skillFlow = Flow.New(reference);
            Skill skill = this.Skill(skillFlow);
            skill.RegisterComputable(name, Func);

            float Func() {
                using var f = Flow.New(reference);
                try {
                    return GetValue(f);
                } catch (Exception e) {
                    var obj = reference.serializedObject;
                    Log.Important?.Error($"[SafeGraph] Exception for graph {obj.name} from ComputableUnit {guid}", obj);
                    Debug.LogException(e);
                    return -1;
                }
            }
        }

        public void StopListening(GraphStack stack) { }

        public bool IsListening(GraphPointer pointer) {
            // It needs to be false so StartListening happens
            return false;
        }

        protected virtual float GetValue(Flow flow) {
            return _value.Value(flow);
        }
    }
}