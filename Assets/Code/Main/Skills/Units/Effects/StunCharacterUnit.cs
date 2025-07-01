using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [UnityEngine.Scripting.Preserve]
    public class StunCharacterUnit : ARUnit {
        protected override void Definition() {
            var status = RequiredARValueInput<Status>("stun status");
            
            DefineSimpleAction(flow => {
                Status statusValue = status.Value(flow);
                statusValue.Character.AddElement(new StunnedCharacterElement(statusValue));
            });
        }
    }
}