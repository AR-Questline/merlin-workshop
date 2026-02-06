using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Steps;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Locations {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class LocationTryChangeAttachmentsGroupUnit : ARUnit {
        protected override void Definition() {
            ARValueInput<Location> inLocation = RequiredARValueInput<Location>("location");
            var groupRef = InlineARValueInput("Group", "");
            var changeToRef = InlineARValueInput("Change", SLocationChangeAttachments.ChangeType.Enable);
            var output = ValueOutput<bool>("success");

            DefineSimpleAction(flow => {
                Location location = inLocation.Value(flow);
                string groupName = groupRef.Value(flow);
                SLocationChangeAttachments.ChangeType type = changeToRef.Value(flow);
                
                bool success = false;
                if (type == SLocationChangeAttachments.ChangeType.Enable) {
                    success = location.TryEnableGroup(groupName);
                } else if (type == SLocationChangeAttachments.ChangeType.Disable) {
                    success = location.TryDisableGroup(groupName);
                }
                flow.SetValue(output, success);
            });
        }
    }
}