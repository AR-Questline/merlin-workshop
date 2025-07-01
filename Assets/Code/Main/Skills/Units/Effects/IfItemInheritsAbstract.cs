using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("Control")]
    [UnityEngine.Scripting.Preserve]
    public class IfItemInheritsAbstract : ARUnit {
        protected override void Definition() {
            var inherits = ControlOutput("inherits");
            var notInherits = ControlOutput("notInherits");
            var itemInput = ValueInput(typeof(Item), "item");
            var abstractInput = ValueInput(typeof(TemplateWrapper<ItemTemplate>), "abstract");
            var enter = ControlInput("enter", flow => {
                var item = itemInput.GetValue<Item>(flow, _ => null);
                var abstractTemplate = abstractInput.GetValue<TemplateWrapper<ItemTemplate>>(flow, _ => null);
                if (item != null && abstractTemplate != null) {
                    if (item.Template.InheritsFrom(abstractTemplate.Template)) {
                        return inherits;
                    }
                }
                return notInherits;
            });
            
            Succession(enter, inherits);
            Succession(enter, notInherits);
        }
    }
}