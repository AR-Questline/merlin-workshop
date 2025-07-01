using Awaken.TG.Main.Character;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class HasBuildupStatusUnit : ARUnit {
        protected override void Definition() {
            var character = RequiredARValueInput<ICharacter>("character");
            ValueOutput("Has Buildup Status", flow => character.Value(flow).Statuses.AllStatuses.Any(s => s.Template.IsBuildupAble));
        }
    }
}