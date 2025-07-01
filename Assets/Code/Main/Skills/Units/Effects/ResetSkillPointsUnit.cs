using Awaken.TG.Main.Heroes;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ResetSkillPointsUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            DefineSimpleAction(_ => {
                Hero.Current?.Talents?.Reset();
            });
        }
    }
}