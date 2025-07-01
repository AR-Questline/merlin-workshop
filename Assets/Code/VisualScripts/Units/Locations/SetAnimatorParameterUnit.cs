using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations;
using Unity.VisualScripting;
using UnityEngine;
using ParamType = UnityEngine.AnimatorControllerParameterType;

// ReSharper disable once CheckNamespace - VisualScripting Compatibility
namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/Animator")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SetAnimatorParameterUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public ParamType type;
        
        protected override void Definition() {
            ARValueInput<Location> inLocation = RequiredARValueInput<Location>("location");
            ARValueInput<string> parameter = InlineARValueInput<string>("parameter", default);
            ARValueInput<float> inFloatValue = null;
            ARValueInput<int> inIntValue = null;
            ARValueInput<bool> inBoolValue = null;
            if (type == ParamType.Float) {
                inFloatValue = InlineARValueInput<float>("value", default);
            } else if (type == ParamType.Int) {
                inIntValue = InlineARValueInput<int>("value", default);
            } else if (type == ParamType.Bool) {
                inBoolValue = InlineARValueInput<bool>("value", default);
            }
            
            DefineSimpleAction(flow => {
                Location location = inLocation.Value(flow);
                int hash = Animator.StringToHash(parameter.Value(flow));
                var savedAnimatorParameter = new SavedAnimatorParameter {
                    type = type,
                    floatValue = inFloatValue?.Value(flow) ?? default,
                    intValue = inIntValue?.Value(flow) ?? default,
                    boolValue = inBoolValue?.Value(flow) ?? default
                };
                location.TryGetElement<AnimatorElement>()?.SetParameter(hash, savedAnimatorParameter);
            });
        }
    }
}