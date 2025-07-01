using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Templates;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Typing {
    [UnitCategory("AR/Templates/Operations")]
    [TypeIcon(typeof(FlowGraph))]
    public abstract class InheritsAbstract<TTemplate, TAbstracted> : ARUnit where TTemplate : class, ITemplate {
        protected override void Definition() {
            var @abstract = RequiredARValueInput<TemplateWrapper<TTemplate>>("abstract");
            var inherited = RequiredARValueInput<TAbstracted>("inherited");
            ValueOutput("", flow => RetrieveTemplate(inherited.Value(flow)).InheritsFrom(@abstract.Value(flow).Template));
        }
        protected abstract TTemplate RetrieveTemplate(TAbstracted abstracted);
    }
    
    [UnityEngine.Scripting.Preserve]
    public class NpcInheritsAbstract : InheritsAbstract<NpcTemplate, NpcElement> {
        protected override NpcTemplate RetrieveTemplate(NpcElement inherited) => inherited.Template;
    }
}