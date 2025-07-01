using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.VisualScripts.Units.VFX {
    public struct VFXEvent : IApplicableToVFX{
        public string name;
        public VFXEventAttributeData attributeData;

        public VFXEvent(string name) {
            this.name = name;
            this.attributeData = new VFXEventAttributeData(new Dictionary<string, bool>(), new Dictionary<string, float>(), 
                new Dictionary<string, int>(), new Dictionary<string, Matrix4x4>(), new Dictionary<string, uint>(), 
                new Dictionary<string, Vector2>(), new Dictionary<string, Vector3>(), new Dictionary<string, Vector4>());
        }
        
        public VFXEvent(string name, VFXEventAttributeData attribute) {
            this.name = name;
            this.attributeData = attribute;
        }

        public void ApplyToVFX(VisualEffect vfx, GameObject gameObject) {
            if (vfx != null) {
                vfx.SendEvent(name, CreateVFXEventAttribute(vfx));
            }
        }
        
        VFXEventAttribute CreateVFXEventAttribute(VisualEffect vfx) {
            var attribute = vfx.CreateVFXEventAttribute();

            foreach (var boolValue in attributeData.boolValues) {
                attribute.SetBool(boolValue.Key, boolValue.Value);
            }
            foreach (var floatValue in attributeData.floatValues) {
                attribute.SetFloat(floatValue.Key, floatValue.Value);
            }
            foreach (var intValue in attributeData.intValues) {
                attribute.SetInt(intValue.Key, intValue.Value);
            }
            foreach (var matrix4X4Value in attributeData.matrix4X4Values) {
                attribute.SetMatrix4x4(matrix4X4Value.Key, matrix4X4Value.Value);
            }
            foreach (var uintValue in attributeData.uintValues) {
                attribute.SetUint(uintValue.Key, uintValue.Value);
            }
            foreach (var vector2Value in attributeData.vector2Values) {
                attribute.SetVector2(vector2Value.Key, vector2Value.Value);
            }
            foreach (var vector3Value in attributeData.vector3Values) {
                attribute.SetVector3(vector3Value.Key, vector3Value.Value);
            }
            foreach (var vector4Value in attributeData.vector4Values) {
                attribute.SetVector4(vector4Value.Key, vector4Value.Value);
            }
            
            return attribute;
        }
    }
}
