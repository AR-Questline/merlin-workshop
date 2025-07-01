using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetLocationsByTagsUnit : ARUnit {
        protected override void Definition() {
            var tags = RequiredARValueInput<string[]>("tags");
            ValueOutput("locations", flow => {
                string[] requiredTags = tags.Value(flow);
                return World.All<Location>().Where(l => TagUtils.HasRequiredTags(l, requiredTags));
            });
        }
    }
    
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetLocationsByTemplateUnit : ARUnit {
        protected override void Definition() {
            var template = RequiredARValueInput<TemplateWrapper<LocationTemplate>>("template");
            ValueOutput("locations", flow => {
                LocationTemplate temp = template.Value(flow).Template;
                return World.All<Location>().Where(l => l.Template == temp);
            });
        }
    }
}