using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    public class SafeGetVariable : ARUnit {

        [Serialize, Inspectable, UnitHeaderInspectable]
        public VariableKind kind;

        InlineValueInput<string> _name;
        FallbackValueInput<GameObject> _object;

        public ValueInput Name => _name.Port;

        protected override void Definition() {
            _name = InlineARValueInput("name", string.Empty);
            
            if (kind == VariableKind.Object) {
                _object = FallbackARValueInput("object", flow => flow.stack.self);
            }
            
            var undefined = ControlOutput("undefined");
            var defined = ControlOutput("defined");

            var value = ValueOutput<object>("value");

            var tryGet = ControlInput("tryGet", flow => {
                var variable = _name.Value(flow);
                var variables = VariableDeclarations(flow);

                if (IsDefined(variables, variable)) {
                    flow.SetValue(value, variables.Get(variable));
                    return defined;
                } else {
                    return undefined;
                }
            });
            
            Succession(tryGet, defined);
            Succession(tryGet, undefined);

            Requirement(_name.Port, tryGet);
        }

        VariableDeclarations VariableDeclarations(Flow flow) {
            return kind switch {
                VariableKind.Flow => flow.variables,
                VariableKind.Graph => Variables.Graph(flow.stack),
                VariableKind.Object => ObjectVariables(_object.Value(flow)),
                VariableKind.Scene => SceneVariables(flow.stack.scene),
                VariableKind.Application => Variables.Application,
                VariableKind.Saved => Variables.Saved,
                _ => throw new ArgumentOutOfRangeException()
            };

            static VariableDeclarations ObjectVariables(GameObject obj) {
                return obj == null ? null : Variables.Object(obj);
            }

            static VariableDeclarations SceneVariables(Scene? scene) {
                if (scene == null || !scene.Value.IsValid() || !scene.Value.isLoaded || !Variables.ExistInScene(scene)) {
                    return null;
                } else {
                    return Variables.Scene(scene.Value);
                }
            }
        }

        static bool IsDefined(VariableDeclarations variables, string name) {
            return !string.IsNullOrEmpty(name) && (variables?.IsDefined(name) ?? false);
        }
    }
}