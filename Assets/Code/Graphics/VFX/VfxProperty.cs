using System;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    // Copy of UnityEngine.VFX.Utility.ExposedProperty but it's a struct
    [Serializable]
    public struct VfxProperty {
        [SerializeField] string m_Name;
#if !UNITY_EDITOR
        int _propertyId;
#endif

        public static implicit operator VfxProperty(string name) {
            return new VfxProperty(name);
        }

        public static explicit operator string(VfxProperty parameter) {
            return parameter.m_Name;
        }

        public static implicit operator int(VfxProperty parameter) {
#if UNITY_EDITOR
            //In Editor, m_Id cached cannot be used for several reasons :
            // - m_Name is modified thought a SerializedProperty
            // - ExposedParameter are stored in array, when we modify it, m_Id is reset to zero
            // - Undo /Redo is restoring m_Name
            // Could be resolved modifying directly object reference in inspector, but for Undo/Redo, we have to invalid everything
            //In Runtime, there isn't any undo/redo and SerializedObject is only available in UnityEditor namespace
            return Shader.PropertyToID(parameter.m_Name);
#else
            if (parameter._propertyId == 0) {
                parameter._propertyId = Shader.PropertyToID(parameter.m_Name);
            }

            return parameter._propertyId;
#endif
        }

        public static VfxProperty operator +(VfxProperty self, VfxProperty other) {
            return new VfxProperty(self.m_Name + other.m_Name);
        }

        VfxProperty(string name) {
            m_Name = name;
#if !UNITY_EDITOR
            _propertyId = 0;
#endif
        }

        public override string ToString() {
            return m_Name;
        }
    }
}
