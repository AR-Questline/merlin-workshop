using System.Collections;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Skills.Units.Listeners;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.VisualScripts.Units.VFX {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class AttributeForVFXEventUnit : ARLoopUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VFXSourceType SourceType { get; set; } = VFXSourceType.MagicVFXWrapper;
        
        RequiredValueInput<GameObject> _vfxGameObject;
        
        InlineValueInput<Dictionary<string, bool>> _boolValues;
        InlineValueInput<Dictionary<string, float>> _floatValues;
        InlineValueInput<Dictionary<string, int>> _intValues;
        InlineValueInput<Dictionary<string, Matrix4x4>> _matrix4X4Values;
        InlineValueInput<Dictionary<string, uint>> _uintValues;
        InlineValueInput<Dictionary<string, Vector2>> _vector2Values;
        InlineValueInput<Dictionary<string, Vector3>> _vector3Values;
        InlineValueInput<Dictionary<string, Vector4>> _vector4Values;
        
        protected override ValueOutput Payload() => SourceType == VFXSourceType.MagicVFXWrapper ? 
            ValueOutput(typeof(VFXEventAttributeData), "vfxEventAttributeData") :
            ValueOutput(typeof(VFXEventAttribute), "vfxEventAttribute");

        protected override IEnumerable Collection(Flow flow) {
            return SourceType == VFXSourceType.MagicVFXWrapper ? CreateAttributeData(flow).Yield() : CreateAttributeFromGameObject(flow).Yield();
        }

        VFXEventAttribute CreateAttributeFromGameObject(Flow flow) {
            return _vfxGameObject.Value(flow)?.GetComponent<VisualEffect>()?.CreateVFXEventAttribute();
        }

        VFXEventAttributeData CreateAttributeData(Flow flow) {
            return new VFXEventAttributeData(_boolValues.Value(flow), _floatValues.Value(flow), _intValues.Value(flow), _matrix4X4Values.Value(flow),
                _uintValues.Value(flow), _vector2Values.Value(flow), _vector3Values.Value(flow), _vector4Values.Value(flow));
        }

        protected override void Definition() {
            if (SourceType == VFXSourceType.GameObject) {
                _vfxGameObject = RequiredARValueInput<GameObject>("VFX GameObject");
            }
            
            _boolValues = InlineARValueInput("boolValue", new Dictionary<string, bool>());
            _floatValues = InlineARValueInput("floatValue", new Dictionary<string, float>());
            _intValues = InlineARValueInput("intValue", new Dictionary<string, int>());
            _matrix4X4Values = InlineARValueInput("matrix4X4Value", new Dictionary<string, Matrix4x4>());
            _uintValues = InlineARValueInput("uintValue", new Dictionary<string, uint>());
            _vector2Values = InlineARValueInput("vector2Value", new Dictionary<string, Vector2>());
            _vector3Values = InlineARValueInput("vector3Value", new Dictionary<string, Vector3>());
            _vector4Values = InlineARValueInput("vector4Value", new Dictionary<string, Vector4>());
            
            base.Definition();
        }
    }
}
