using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Tags;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Utils.Items {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory("AR/Utils/Items")]
    [UnitTitle("Filter Item By Tags")]
    [UnityEngine.Scripting.Preserve]
    public class FilterItemByTagsUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int count;
        
        protected override void Definition() {
            var tags = new ARValueInput<string>[count];
            for (int i = 0; i < count; i++) {
                tags[i] = InlineARValueInput("tag_" + i, "");
            }
            if (count == 0) {
                ValueOutput<Func<Item, bool>>("filter", _ => _ => false);
            } else if (count == 1) {
                ValueOutput<Func<Item, bool>>("filter", flow => {
                    var tag = tags[0].Value(flow);
                    return item => TagUtils.HasRequiredTag(item.Tags, tag);
                });
            } else {
                ValueOutput<Func<Item, bool>>("filter", flow => {
                    var tagsValue = new string[count];
                    for (int i = 0; i < count; i++) {
                        tagsValue[i] = tags[i].Value(flow);
                    }
                    return item => TagUtils.HasRequiredTags(item.Tags, tagsValue);
                });
            }
        }
    }
}