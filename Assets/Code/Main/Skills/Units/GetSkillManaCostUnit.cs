using Awaken.TG.Main.Heroes.Combat;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetManaCostUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("lightManaCost", this.GetLightManaCost);
            ValueOutput("heavyManaCost", this.GetHeavyManaCost);
            ValueOutput("heavyManaCostPerSecond", this.GetHeavyManaCostPerSecond);
        }
    }
}