using Awaken.TG.Main.Stories;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetFlagUnit : ARUnit {
        protected override void Definition() {
            var flagInput = InlineARValueInput("flag", "");

            ValueOutput("value", flow => StoryFlags.Get(flagInput.Value(flow)));
        }
    }
}