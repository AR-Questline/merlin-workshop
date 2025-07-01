using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ChangeSkillChargeAbleUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var setChargeAbleInput = RequiredARValueInput<bool>("setChargeAble");
            
            DefineSimpleAction("Enter", "Exit", flow => {
                bool setChargeAble = setChargeAbleInput.Value(flow);
                var item = this.Skill(flow).SourceItem;
                if (setChargeAble) {
                    item.RemoveMarkerElement<DisableSkillChargeMarker>();
                } else {
                    item.AddMarkerElement<DisableSkillChargeMarker>();
                }
            });
        }
    }
}