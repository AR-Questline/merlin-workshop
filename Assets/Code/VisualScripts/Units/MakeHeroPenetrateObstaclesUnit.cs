using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Skills;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class MakeHeroPenetrateObstaclesUnit : ARUnit, ISkillUnit {
        InlineValueInput<bool> _canPenetrate;
        
        protected override void Definition() {
            _canPenetrate = new InlineValueInput<bool>(ValueInput("can Penetrate", true));
            var obstacleTypesToPenetrate = FallbackARValueInput("obstacleTypesToPenetrate", _ => GameConstants.Get.defaultHeroPenetratingObstaclesMask);
            
            DefineSimpleAction(flow => {
                Hero.Current?.VHeroController?.SetExcludedLayerMaskOverride(this.Skill(flow).ID, _canPenetrate.Value(flow), obstacleTypesToPenetrate.Value(flow));
            });
        }
    }
}