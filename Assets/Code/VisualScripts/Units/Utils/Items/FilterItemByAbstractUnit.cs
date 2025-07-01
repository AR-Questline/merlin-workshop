using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Utils.Items {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory("AR/Utils/Items")]
    [UnitTitle("Filter Item By Abstract")]
    [UnityEngine.Scripting.Preserve]
    public class FilterItemByAbstractUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int count;
        protected override void Definition() {
            var abstractPorts = new ARValueInput<TemplateWrapper<ItemTemplate>>[count];
            for (int i = 0; i < count; i++) {
                abstractPorts[i] = RequiredARValueInput<TemplateWrapper<ItemTemplate>>("abstract_" + i);
            }
            if (count == 0) {
                ValueOutput<Func<Item, bool>>("filter", _ => _ => false);
            } else if (count == 1) {
                ValueOutput<Func<Item, bool>>("filter", flow => {
                    var abstractTemplate = abstractPorts[0].Value(flow).Template;
                    return item => item.Template != null && item.Template.InheritsFrom(abstractTemplate);
                });
            } else {
                ValueOutput<Func<Item, bool>>("filter", flow => {
                    var abstractTemplates = new ItemTemplate[count];
                    for (int i = 0; i < count; i++) {
                        abstractTemplates[i] = abstractPorts[i].Value(flow).Template;
                    }
                    return item => {
                        if (item.Template != null) {
                            foreach (var abstractTemplate in abstractTemplates) {
                                if (!item.Template.InheritsFrom(abstractTemplate)) {
                                    return false;
                                }
                            }
                        }
                        return true;
                    };
                });
            }
        }
    }
}