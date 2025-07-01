using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Utils.Items {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory("AR/Utils/Items")]
    [UnitTitle("Filter Item By Template")]
    [UnityEngine.Scripting.Preserve]
    public class FilterItemByTemplateUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int count;
        
        protected override void Definition() {
            var templatePorts = new ARValueInput<TemplateWrapper<ItemTemplate>>[count];
            for (int i = 0; i < count; i++) {
                templatePorts[i] = RequiredARValueInput<TemplateWrapper<ItemTemplate>>("template_" + i);
            }
            if (count == 0) {
                ValueOutput<Func<Item, bool>>("filter", _ => _ => false);
            } else if (count == 1) {
                ValueOutput<Func<Item, bool>>("filter", flow => {
                    var template = templatePorts[0].Value(flow).Template;
                    return item => item.Template != null && item.Template == template;
                });
            } else {
                ValueOutput<Func<Item, bool>>("filter", flow => {
                    var templates = new ItemTemplate[count];
                    for (int i = 0; i < count; i++) {
                        templates[i] = templatePorts[i].Value(flow).Template;
                    }
                    return item => {
                        if (item.Template != null) {
                            foreach (var template in templates) {
                                if (item.Template == template) {
                                    return true;
                                }
                            }
                        }
                        return false;
                    };
                });
            }
        }
    }
}