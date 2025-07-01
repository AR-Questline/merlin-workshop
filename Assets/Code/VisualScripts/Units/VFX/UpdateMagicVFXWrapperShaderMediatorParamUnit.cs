using Awaken.TG.Main.Heroes.Combat;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.VisualScripts.Units.VFX {
    [UnitCategory("AR/VFX_Systems")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class UpdateMagicVFXWrapperShaderMediatorParamUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VFXSourceType SourceType { get; set; } = VFXSourceType.MagicVFXWrapper;
        
        RequiredValueInput<MagicVFXWrapper> _vfxWrapper;
        RequiredValueInput<GameObject> _vfxGameObject;
        RequiredValueInput<float> _duration;

        protected override void Definition() {
            switch (SourceType) {
                case VFXSourceType.MagicVFXWrapper:
                    _vfxWrapper = RequiredARValueInput<MagicVFXWrapper>("VFX Wrapper");
                    break;
                case VFXSourceType.GameObject:
                    _vfxGameObject = RequiredARValueInput<GameObject>("VFX GameObject");
                    break;
            }
            _duration = RequiredARValueInput<float>("Duration");
            DefineSimpleAction(SendEvent);
        }

        void SendEvent(Flow flow) {
            var param = new MagicVFXShaderMediatorParam(_duration.Value(flow));
            if (SourceType == VFXSourceType.MagicVFXWrapper) {
                _vfxWrapper.Value(flow).UpdateShaderControllerMediator(param);
            } else {
                GameObject go = _vfxGameObject.Value(flow);
                if (go != null) {
                    param.ApplyToVFX(go.GetComponentInChildren<VisualEffect>(), go);
                }
            }
        }
    }
}
