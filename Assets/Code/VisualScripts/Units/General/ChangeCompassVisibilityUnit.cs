using System;
using System.Linq;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/UI")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ChangeCompassVisibilityUnit : ARUnit {
        protected override void Definition() {
            var idInput = InlineARValueInput("id", string.Empty);
            var visibleInput = InlineARValueInput("visible", false);
            DefineSimpleAction(flow => {
                string id = idInput.Value(flow);
                if (string.IsNullOrWhiteSpace(id)) {
                    throw new ArgumentException("You need to specify ID of the compass visibility change");
                }

                if (visibleInput.Value(flow)) {
                    World.All<HideCompass>().FirstOrDefault(x => x.SourceID == id)?.Discard();
                } else {
                    if (World.All<HideCompass>().All(x => x.SourceID != id)) {
                        World.Add(new HideCompass(id));
                    }
                }
            });
        }
    }
}