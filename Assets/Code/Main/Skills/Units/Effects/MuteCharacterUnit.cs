using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.VisualScripts.Units;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnityEngine.Scripting.Preserve]
    public class MuteCharacterUnit : ARUnit {
        protected override void Definition() {
            var status = RequiredARValueInput<Status>("mute status");
            
            DefineSimpleAction(flow => {
                Status statusValue = status.Value(flow);
                statusValue.Character.AddElement(new MutedMarker(statusValue));
            });
        }
    }
}