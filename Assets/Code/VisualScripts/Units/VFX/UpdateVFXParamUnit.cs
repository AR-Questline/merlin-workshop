using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.VFX {
    [UnitCategory("AR/VFX_Systems")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class UpdateVFXParamUnit : BaseUpdateVFXParamUnit {
        RequiredValueInput<Item> _inItem;
        
        protected override void Definition() {
            _inItem = RequiredARValueInput<Item>("itemOwningVFX");
            base.Definition();
        }

        protected override void SetMagicVfxParam(Flow flow, MagicVFXParam magicVFXParam) {
            _inItem.Value(flow).Trigger(VCCharacterMagicVFX.Events.ChangeMagicVFXParam, magicVFXParam);
        }
    }
}