using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.VFX {
    public struct VFXEventAttributeData {
        public Dictionary<string, bool> boolValues;
        public Dictionary<string, float> floatValues;
        public Dictionary<string, int> intValues;
        public Dictionary<string, Matrix4x4> matrix4X4Values;
        public Dictionary<string, uint> uintValues;
        public Dictionary<string, Vector2> vector2Values;
        public Dictionary<string, Vector3> vector3Values;
        public Dictionary<string, Vector4> vector4Values;
        
        public VFXEventAttributeData(Dictionary<string, bool> boolValues, Dictionary<string, float> floatValues, Dictionary<string, int> intValues, Dictionary<string, Matrix4x4> matrix4X4Values, Dictionary<string, uint> uintValues, Dictionary<string, Vector2> vector2Values, Dictionary<string, Vector3> vector3Values, Dictionary<string, Vector4> vector4Values) {
            this.boolValues = boolValues;
            this.floatValues = floatValues;
            this.intValues = intValues;
            this.matrix4X4Values = matrix4X4Values;
            this.uintValues = uintValues;
            this.vector2Values = vector2Values;
            this.vector3Values = vector3Values;
            this.vector4Values = vector4Values;
        }
    }
}