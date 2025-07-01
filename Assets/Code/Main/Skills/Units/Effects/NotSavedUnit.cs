using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class NotSavedUnit : ARUnit {
        protected override void Definition() {
            var inModel = RequiredARValueInput<IModel>("model");
            DefineSimpleAction(flow => {
                IModel model = inModel.Value(flow);
                model.MarkedNotSaved = true;
            });
        }
    }
}