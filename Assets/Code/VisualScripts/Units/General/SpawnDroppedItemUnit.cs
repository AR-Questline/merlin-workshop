using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SpawnDroppedItemUnit : ARUnit {
        protected override void Definition() {
            var spawnPos = ValueInput(typeof(Vector3), "spawnPosition");
            var itemTemplate = ValueInput(typeof(TemplateWrapper<ItemTemplate>), "itemTemplate");
            var amount = FallbackARValueInput("amount", _ => 1);
            var itemLevel = FallbackARValueInput("itemLevel", _ => 0);
            var output = ValueOutput<Location>("spawnedLocation");
            DefineSimpleAction(f => {
                Vector3 pos = f.GetValue<Vector3>(spawnPos);
                ItemTemplate template = f.GetValue<TemplateWrapper<ItemTemplate>>(itemTemplate).Template;
                int quantity = amount.Value(f);
                int level = itemLevel.Value(f);
                Location spawnedLocation = DroppedItemSpawner.SpawnDroppedItemPrefab(pos, template, quantity, level);
                f.SetValue(output, spawnedLocation);
            });
        }
    }
}
