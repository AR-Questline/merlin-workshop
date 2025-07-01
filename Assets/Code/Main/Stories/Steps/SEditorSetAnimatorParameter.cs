using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Set Animator Parameter"), NodeSupportsOdin]
    public class SEditorSetAnimatorParameter : EditorStep {
        [InfoBox("Not a recommended way of changing animator state - use Emit Logic instead!", InfoMessageType.Error)]
        
        public LocationReference location;
        [OnValueChanged(nameof(UpdateHash))] [UnityEngine.Scripting.Preserve] public string parameterName;
        public int parameterHash;
        [HideInInspector]
        public AnimatorControllerParameterType type;
        [InlineProperty, HideLabel]
        public SavedAnimatorParameter parameter;

        void UpdateHash(string newName) {
            parameterHash = Animator.StringToHash(newName);
        }

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SSetAnimatorParameter {
                location = location,
                parameterHash = parameterHash,
                type = type,
                parameter = parameter
            };
        }
    }

    public partial class SSetAnimatorParameter : StoryStepWithLocationRequirement {
        public LocationReference location;
        public int parameterHash;
        public AnimatorControllerParameterType type;
        public SavedAnimatorParameter parameter;

        protected override LocationReference RequiredLocations => location;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            if (parameter.type == 0) {
                parameter.type = type;
            }
            return new StepExecution(parameterHash, parameter);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_SetAnimatorParameter;

            [Saved] int _parameterHash; 
            [Saved] SavedAnimatorParameter _parameter;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            internal StepExecution(int parameterHash, SavedAnimatorParameter parameter) {
                _parameterHash = parameterHash;
                _parameter = parameter;
            }
            
            public override void Execute(Location location) {
                location.TryGetElement<AnimatorElement>()?.SetParameter(_parameterHash, _parameter);
            }
        }
    }
}