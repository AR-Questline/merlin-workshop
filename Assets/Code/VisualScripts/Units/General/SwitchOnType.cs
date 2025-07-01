using System;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General")]
    [TypeIcon(typeof(SwitchOnInteger))]
    [UnityEngine.Scripting.Preserve]
    public class SwitchOnType : ARUnit {

        [Inspectable, Serialize] public List<Type> types = new();

        ControlInput _enter;
        RequiredValueInput<object> _input;
        List<KeyValuePair<Type, ControlOutput>> _branches;
        ControlOutput _default;

        public override bool canDefine => types != null;

        protected override void Definition() {
            _branches = new List<KeyValuePair<Type, ControlOutput>>();

            _enter = ControlInput("enter", Enter);
            _input = RequiredARValueInput<object>("input");
            
            foreach (var type in types) {
                if (type != null) {
                    var key = "%" + type.Name;
                    if (!controlOutputs.Contains(key)) {
                        var branch = ControlOutput(key);
                        _branches.Add(new KeyValuePair<Type, ControlOutput>(type, branch));
                        Succession(_enter, branch);
                    }
                }
            }
            
            _default = ControlOutput("default");
            Succession(_enter, _default);
        }

        ControlOutput Enter(Flow flow) {
            var type = _input.Value(flow)?.GetType();
            if (type == null) return _default;
                
            foreach (var (t, output) in _branches) {
                if (t.IsAssignableFrom(type)) {
                    return output;
                }
            }

            return _default;
        }
    }
}