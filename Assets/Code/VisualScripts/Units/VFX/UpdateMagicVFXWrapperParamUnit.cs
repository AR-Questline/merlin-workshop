using Awaken.TG.Main.Heroes.Combat;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.VFX {
    [UnitCategory("AR/VFX_Systems")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class UpdateMagicVFXWrapperParamUnit : BaseUpdateVFXParamUnit {
        RequiredValueInput<MagicVFXWrapper> _magicVfxWrapper;

        protected override void Definition() {
            _magicVfxWrapper = RequiredARValueInput<MagicVFXWrapper>("magicVfxWrapper");
            base.Definition();
        }

        protected override void SetMagicVfxParam(Flow flow, MagicVFXParam magicVFXParam) {
            MagicVFXWrapper magicVFXWrapper = _magicVfxWrapper.Value(flow);
            if (magicVFXWrapper == null) {
                Log.Important?.Warning($"{flow.stack?.rootGraph} MagicVFXWrapper is null");
                return;
            }
            magicVFXWrapper.UpdateMagicVfxParams(magicVFXParam);
        }
    }
}