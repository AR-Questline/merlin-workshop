using Awaken.TG.Main.Heroes.Combat;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.VisualScripts.Units.VFX {
    [UnitCategory("AR/VFX_Systems")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SendEventToVFXUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VFXSourceType SourceType { get; set; } = VFXSourceType.MagicVFXWrapper;
        
        RequiredValueInput<MagicVFXWrapper> _vfxWrapper;
        RequiredValueInput<GameObject> _vfxGameObject;
        RequiredValueInput<string> _eventName;
        OptionalValueInput<VFXEventAttribute> _gameObjectEventAttribute;
        OptionalValueInput<VFXEventAttributeData> _wrapperEventAttributeData;
        
        protected override void Definition() {
            switch (SourceType) {
                case VFXSourceType.MagicVFXWrapper:
                    _vfxWrapper = RequiredARValueInput<MagicVFXWrapper>("VFX Wrapper");
                    _wrapperEventAttributeData = OptionalARValueInput<VFXEventAttributeData>("Event Attribute Data");
                    break;
                case VFXSourceType.GameObject:
                    _vfxGameObject = RequiredARValueInput<GameObject>("VFX GameObject");
                    _gameObjectEventAttribute = OptionalARValueInput<VFXEventAttribute>("Event Attribute");
                    break;
            }
            
            _eventName = RequiredARValueInput<string>("Event Name");
            DefineSimpleAction(SendEvent);
        }

        void SendEvent(Flow flow) {
            if (SourceType == VFXSourceType.MagicVFXWrapper) {
                if (_wrapperEventAttributeData.HasValue) {
                    _vfxWrapper.Value(flow).SendEvent(_eventName.Value(flow), _wrapperEventAttributeData.Value(flow));
                } else {
                    _vfxWrapper.Value(flow).SendEvent(_eventName.Value(flow));
                }
            } else {
                VisualEffect vfx = _vfxGameObject.Value(flow).GetComponent<VisualEffect>();
                
                if (vfx != null) {
                    if (_gameObjectEventAttribute.HasValue) {
                        vfx.SendEvent(_eventName.Value(flow), _gameObjectEventAttribute.Value(flow));
                    } else {
                        vfx.SendEvent(_eventName.Value(flow));
                    }
                }
            }
        }
    }
}
