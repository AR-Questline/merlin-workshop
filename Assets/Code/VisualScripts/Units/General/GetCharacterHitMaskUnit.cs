using Awaken.TG.Main.Character;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetCharacterHitMaskUnit : ARUnit {
        protected override void Definition() {
            var characterInput = RequiredARValueInput<ICharacter>("character");
            ValueOutput("value", flow => characterInput.Value(flow).GetHandOwner()?.HitLayerMask ?? ~0);
        }
    }
}