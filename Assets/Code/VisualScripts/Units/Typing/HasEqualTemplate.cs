using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Templates;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Typing {
    [UnitCategory("AR/Templates/Operations")]
    [TypeIcon(typeof(FlowGraph))]
    public abstract class HasEqualTemplate<TTemplate, TTemplated> : ARUnit where TTemplate : class, ITemplate {
        protected override void Definition() {
            var template = RequiredARValueInput<TemplateWrapper<TTemplate>>("template");
            var templated = RequiredARValueInput<TTemplated>("templated");
            ValueOutput("", flow => Equals(RetrieveTemplate(templated.Value(flow)), template.Value(flow).Template));
        }
        protected abstract TTemplate RetrieveTemplate(TTemplated templated);
    }
    
    [UnityEngine.Scripting.Preserve]
    public class HasEqualNpcTemplate : HasEqualTemplate<NpcTemplate, NpcElement> {
        protected override NpcTemplate RetrieveTemplate(NpcElement templated) => templated.Template;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class HasEqualFactionTemplate : HasEqualTemplate<FactionTemplate, NpcElement> {
        protected override FactionTemplate RetrieveTemplate(NpcElement templated) => templated.Faction.Template;
    }
}