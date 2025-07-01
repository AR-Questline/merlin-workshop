using Awaken.TG.Main.General.Features;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/Technical")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SaveGameUnit : ARUnit {
        protected override void Definition() {
            DefineSimpleAction(flow => {
                if (LoadSave.Get.CanAutoSave()) {
                    LoadSave.Get.Save(SaveSlot.GetAutoSave());
                } else {
                    Log.Important?.Warning("Cannot save now! Graph: " + flow.stack.graph.title);
                }
            });
        }
    }
}