using Awaken.TG.Main.Heroes;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ApplySpecialPostProcessUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var inPPType = RequiredARValueInput<SpecialPostProcessType>("ppType");
            var inEnableTime = FallbackARValueInput("enableBlendDuration", _ => 0.5f);
            var inDisableTime = FallbackARValueInput("disableBlendDuration", _ => 0.5f);
            
            var ppOutput = ValueOutput(typeof(HeroSpecialPostProcess), "SpecialPostProcessElement");
            
            DefineSimpleAction("enter", "exit", flow => {
                var ppType = inPPType.Value(flow);
                var enableTime = inEnableTime.Value(flow);
                var disableTime = inDisableTime.Value(flow);

                var ppElement = this.Skill(flow).AddElement(new HeroSpecialPostProcess(ppType, enableTime, disableTime));
                flow.SetValue(ppOutput, ppElement);
            });
        }
    }
}