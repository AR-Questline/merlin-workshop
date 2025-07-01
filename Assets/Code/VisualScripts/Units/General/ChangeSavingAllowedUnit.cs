using System;
using System.Linq;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/Technical")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ChangeSavingAllowedUnit : ARUnit {
        protected override void Definition() {
            var idInput = InlineARValueInput("id", string.Empty);
            var visibleInput = InlineARValueInput("visible", false);
            DefineSimpleAction(flow => {
                string id = idInput.Value(flow);
                if (string.IsNullOrWhiteSpace(id)) {
                    throw new ArgumentException("You need to specify ID of the saving allowed change");
                }

                if (visibleInput.Value(flow)) {
                    World.All<SaveBlocker>().FirstOrDefault(b => b.SourceID == id)?.Discard();
                } else {
                    if (World.All<SaveBlocker>().All(b => b.SourceID != id)) {
                        World.Add(new SaveBlocker(id));
                    }
                }
            });
        }
    }
}