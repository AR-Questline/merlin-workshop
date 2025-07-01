using System.ComponentModel;
using UnityEngine;

namespace Awaken.TG.Main.Saving.DefaultValues {
    public class DefaultValueQuaternionAttribute : DefaultValueAttribute {
        public DefaultValueQuaternionAttribute(float x, float y, float z, float w) : base(new Quaternion(x, y, z, w)) { }
    }
    
    public class DefaultValueQuaternionIdentityAttribute : DefaultValueQuaternionAttribute {
        public DefaultValueQuaternionIdentityAttribute() : base(0, 0, 0, 1) { }
    }
}