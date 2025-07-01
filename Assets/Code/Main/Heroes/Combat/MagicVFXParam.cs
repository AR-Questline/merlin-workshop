using System;
using Awaken.Kandra;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Graphics.VFX.Binders;
using Awaken.TG.VisualScripts.Units.VFX;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Heroes.Combat {
    public struct MagicVFXParam : IApplicableToVFXWithMaterial {
        const int MaxValueValueSize = 4*4; // 4 floats

        readonly VFXParamType _type;
        readonly int _nameId;

        object _refValue;
        unsafe fixed byte _valueValue[MaxValueValueSize]; // 4 floats

        MagicVFXParam(string name, VFXParamType type) : this() {
            _nameId = Shader.PropertyToID(name);
            _type = type;
        }
        
        public static MagicVFXParam Float(string name, float value) {
            return new MagicVFXParam(name, VFXParamType.Float).SetValueValue(value);
        }
        
        public static MagicVFXParam Int(string name, int value) {
            return new MagicVFXParam(name, VFXParamType.Int).SetValueValue(value);
        }

        public static MagicVFXParam UInt(string name, uint value) {
            return new MagicVFXParam(name, VFXParamType.UInt).SetValueValue(value);
        }
        
        public static MagicVFXParam Bool(string name, bool value) {
            return new MagicVFXParam(name, VFXParamType.Bool).SetValueValue(value);
        }

        public static MagicVFXParam Vector2(string name, Vector2 value) {
            return new MagicVFXParam(name, VFXParamType.Vector2).SetValueValue(value);
        }

        public static MagicVFXParam Vector3(string name, Vector3 value) {
            return new MagicVFXParam(name, VFXParamType.Vector3).SetValueValue(value);
        }

        public static MagicVFXParam Vector4(string name, Vector4 value) {
            return new MagicVFXParam(name, VFXParamType.Vector4).SetValueValue(value);
        }
        
        public static MagicVFXParam Gradient(string name, Gradient value) {
            return new MagicVFXParam(name, VFXParamType.Gradient).SetRefValue(value);
        }
        
        public static MagicVFXParam Mesh(string name, Mesh value) {
            Log.Important?.Error("Tell programmer that you want to use mesh in VFX so he can implement it properly");
            return new MagicVFXParam(name, VFXParamType.Mesh).SetRefValue(value);
        }
        
        public static MagicVFXParam Texture(string name, Texture value) {
            return new MagicVFXParam(name, VFXParamType.Texture).SetRefValue(value);
        }
        
        public static MagicVFXParam AnimationCurve(string name, AnimationCurve value) {
            return new MagicVFXParam(name, VFXParamType.AnimationCurve).SetRefValue(value);
        }

        public static MagicVFXParam KandraRenderer(string name, KandraRenderer value) {
            var vfxBodyMarker = value.GetComponent<VFXBodyMarker>();
            return new MagicVFXParam(name, VFXParamType.KandraRenderer).SetRefValue(vfxBodyMarker);
        }

        public static MagicVFXParam VFXEvent(string name, VFXEventAttributeData? value) {
            VFXEvent vfxEvent = value == null ? new VFXEvent(name) : new VFXEvent(name, value.Value);
            return new MagicVFXParam(name, VFXParamType.Event).SetRefValue(vfxEvent);
        }

        public void SetVisualEffectParam(VisualEffect visualEffect) {
            if (_type == VFXParamType.Float) {
                if (visualEffect.HasFloat(_nameId)) {
                    visualEffect.SetFloat(_nameId, GetValueValue<float>());
                }
            } else if (_type == VFXParamType.Int) {
                if (visualEffect.HasInt(_nameId)) {
                    visualEffect.SetInt(_nameId, GetValueValue<int>());
                }
            } else if (_type == VFXParamType.UInt) {
                if (visualEffect.HasUInt(_nameId)) {
                    visualEffect.SetUInt(_nameId, GetValueValue<uint>());
                }
            } else if (_type == VFXParamType.Bool) {
                if (visualEffect.HasBool(_nameId)) {
                    visualEffect.SetBool(_nameId, GetValueValue<bool>());
                }
            } else if (_type == VFXParamType.Vector2) {
                if (visualEffect.HasVector2(_nameId)) {
                    visualEffect.SetVector2(_nameId, GetValueValue<Vector2>());
                }
            } else if (_type == VFXParamType.Vector3) {
                if (visualEffect.HasVector3(_nameId)) {
                    visualEffect.SetVector3(_nameId, GetValueValue<Vector3>());
                }
            } else if (_type == VFXParamType.Vector4) {
                if (visualEffect.HasVector4(_nameId)) {
                    visualEffect.SetVector4(_nameId, GetValueValue<Vector4>());
                }
            } else if (_type == VFXParamType.Gradient) {
                if (visualEffect.HasGradient(_nameId)) {
                    visualEffect.SetGradient(_nameId, _refValue as Gradient);
                }
            } else if (_type == VFXParamType.Mesh) {
                if (visualEffect.HasMesh(_nameId)) {
                    visualEffect.SetMesh(_nameId, _refValue as Mesh);
                }
            } else if (_type == VFXParamType.Texture) {
                if (visualEffect.HasTexture(_nameId)) {
                    visualEffect.SetTexture(_nameId, _refValue as Texture);
                }
            } else if (_type == VFXParamType.AnimationCurve) {
                if (visualEffect.HasAnimationCurve(_nameId)) {
                    visualEffect.SetAnimationCurve(_nameId, _refValue as AnimationCurve);
                }
            } else if (_type == VFXParamType.KandraRenderer) {
                var binder = visualEffect.GetComponent<VFXBodyMarkerBinder>();
                if (!binder) {
                    Log.Important?.Error($"VFX {visualEffect.name} does not have {nameof(VFXBodyMarkerBinder)}, so cannot be used with KandraRenderer set param", visualEffect);
                    return;
                }
                if (_refValue is VFXBodyMarker vfxBodyMarker) {
                    binder.SetBody(vfxBodyMarker);
                    VFXBodyLifetimeGuard.Add(visualEffect.gameObject, vfxBodyMarker, false);
                }
            } else if (_type == VFXParamType.Event) {
                if (_refValue is VFXEvent data) {
                    data.ApplyToVFX(visualEffect, null);
                } else {
                    visualEffect.SendEvent(_nameId);
                }
            } else {
                throw new NotImplementedException();
            }
        }

        public void SetMaterialParam(Material material) {
            if (_type == VFXParamType.Float) {
                if (material.HasFloat(_nameId)) {
                    material.SetFloat(_nameId, GetValueValue<float>());
                }
            } else if (_type == VFXParamType.Int) {
                if (material.HasInteger(_nameId)) {
                    material.SetInteger(_nameId, GetValueValue<int>());
                }
            } else if (_type == VFXParamType.UInt) {
                if (material.HasInteger(_nameId)) {
                    material.SetInteger(_nameId, (int)GetValueValue<uint>());
                }
            } else if (_type == VFXParamType.Bool) {
                if (material.HasInteger(_nameId)) {
                    material.SetInteger(_nameId, GetValueValue<bool>() ? 1 : 0);
                }
            } else if (_type == VFXParamType.Vector2) {
                if (material.HasVector(_nameId)) {
                    material.SetVector(_nameId, GetValueValue<Vector2>());
                }
            } else if (_type == VFXParamType.Vector3) {
                if (material.HasVector(_nameId)) {
                    material.SetVector(_nameId, GetValueValue<Vector3>());
                }
            } else if (_type == VFXParamType.Vector4) {
                if (material.HasVector(_nameId)) {
                    material.SetVector(_nameId, GetValueValue<Vector4>());
                }
            } else if (_type == VFXParamType.Texture) {
                if (material.HasTexture(_nameId)) {
                    material.SetTexture(_nameId, _refValue as Texture);
                }
            }
        }

        public void ApplyToVFX(VisualEffect vfx, GameObject gameObject) {
            if (vfx != null) {
                SetVisualEffectParam(vfx);
            }
        }
        
        public void ApplyToShaderMaterial(Material shaderMaterial, GameObject gameObject) {
            SetMaterialParam(shaderMaterial);
        }

        unsafe MagicVFXParam SetValueValue<T>(T value) where T : unmanaged {
            fixed (byte* originalDataPtr = _valueValue) {
                var data = (T*)originalDataPtr;
                *data = value;
            }

            _refValue = null;
            return this;
        }

        MagicVFXParam SetRefValue(object value) {
            _refValue = value;
            return this;
        }

        unsafe T GetValueValue<T>() where T : unmanaged {
            fixed (byte* originalDataPtr = _valueValue) {
                return *(T*)originalDataPtr;
            }
        }

        public enum VFXParamType : byte {
            Float = 0,
            Int = 1,
            Bool = 2,
            Gradient = 3,
            Mesh = 4,
            Texture = 5,
            Vector2 = 6,
            Vector3 = 7,
            Vector4 = 8,
            AnimationCurve = 9,
            SkinnedMeshRenderer = 10,
            UInt = 11,
            KandraRenderer = 12,
            Event = 13,
        }
    }
}