using Awaken.Kandra;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.VFX {
    public abstract class BaseUpdateVFXParamUnit : ARUnit {
        InlineValueInput<string> _inParam;

        [Serialize, Inspectable, UnitHeaderInspectable]
        public MagicVFXParam.VFXParamType ParamType { get; set; } = MagicVFXParam.VFXParamType.Float;
        
        protected override void Definition() {
            _inParam = InlineARValueInput("paramName", string.Empty);
            
            switch (ParamType) {
                case MagicVFXParam.VFXParamType.Float:
                    var inFloatValue = InlineARValueInput("floatValue", 0f);
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Float(_inParam.Value(flow), inFloatValue.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.UInt:
                    var inUIntValue = InlineARValueInput("uintValue", 0u);
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.UInt(_inParam.Value(flow), inUIntValue.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.Int:
                    var inIntValue = InlineARValueInput("intValue", 0);
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Int(_inParam.Value(flow), inIntValue.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.Bool:
                    var inBoolValue = InlineARValueInput("boolValue", false);
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Bool(_inParam.Value(flow), inBoolValue.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.Vector2:
                    var inVector2 = InlineARValueInput("vector2Value", default(Vector2));
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Vector2(_inParam.Value(flow), inVector2.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.Vector3:
                    var inVector3 = InlineARValueInput("vector3Value", default(Vector3));
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Vector3(_inParam.Value(flow), inVector3.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.Vector4:
                    var inVector4 = InlineARValueInput("vector4Value", default(Vector4));
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Vector4(_inParam.Value(flow), inVector4.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.Gradient:
                    var inGradientValue = InlineARValueInput("gradientValue", default(Gradient));
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Gradient(_inParam.Value(flow), inGradientValue.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.Mesh:
                    var inMeshValue = RequiredARValueInput<Mesh>("meshValue");
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Mesh(_inParam.Value(flow), inMeshValue.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.Texture:
                    var inTexture = RequiredARValueInput<Texture>("textureValue");
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.Texture(_inParam.Value(flow), inTexture.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.AnimationCurve:
                    var inAnimationCurve = RequiredARValueInput<AnimationCurve>("animationCurveValue");
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.AnimationCurve(_inParam.Value(flow), inAnimationCurve.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.KandraRenderer:
                    var inKandraParam = RequiredARValueInput<KandraRenderer>("kandraRendererValue");
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.KandraRenderer(_inParam.Value(flow), inKandraParam.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                case MagicVFXParam.VFXParamType.SkinnedMeshRenderer:
                    Log.Minor?.Error($"Skinned mesh renderer is obsolete, use Kandra instead for VFX, for: {this}-{graph}-{guid}");
                    break;
                case MagicVFXParam.VFXParamType.Event:
                    var inAttributeData = FallbackARValueInput<VFXEventAttributeData?>("attributeData", _ => null);
                    DefineSimpleAction(flow => {
                        var magicVFXParam = MagicVFXParam.VFXEvent(_inParam.Value(flow), inAttributeData.Value(flow));
                        SetMagicVfxParam(flow, magicVFXParam);
                    });
                    break;
                default:
                    Log.Minor?.Error($"Undefined VFX param type: {ParamType}, for: {{this}}-{{graph}}-{{guid}}");
                    break;
            }
        }

        protected abstract void SetMagicVfxParam(Flow flow, MagicVFXParam magicVFXParam);
    }
}