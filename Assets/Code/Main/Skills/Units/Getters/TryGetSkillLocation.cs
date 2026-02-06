using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Getters")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class TryGetSkillLocation : ARUnit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("Location", TryFindLocationInAnyParents);
            ValueOutput("Location GameObject", flow => TryFindLocationInAnyParents(flow)?.LocationView.gameObject);
        }
        
        Location TryFindLocationInAnyParents(Flow flow) {
            var skill = this.Skill(flow);
            var currentModel = skill.ParentModel as Element;
            
            while (currentModel != null) {
                if (currentModel is Element<Location> locationElement) {
                    return locationElement.ParentModel;
                }
                currentModel = currentModel.GenericParentModel as Element;
            }
            return null;
        }
    }
}