using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Steps;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Locations {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class LocationAttachmentsGroupUnit : ARUnit {
        protected override void Definition() {
            ARValueInput<Location> inLocation = RequiredARValueInput<Location>("location");
            var groupRef = InlineARValueInput("Group", "");
            var changeToRef = InlineARValueInput("Change", SLocationChangeAttachments.ChangeType.Enable);
            

            DefineSimpleAction(flow => {
                Location location = inLocation.Value(flow);
                string groupName = groupRef.Value(flow);
                SLocationChangeAttachments.ChangeType type = changeToRef.Value(flow);
                if (type == SLocationChangeAttachments.ChangeType.Enable) {
                    location.EnableGroup(groupName);
                } else if (type == SLocationChangeAttachments.ChangeType.Disable) {
                    location.DisableGroup(groupName);
                }
            });
        }
    }
}
