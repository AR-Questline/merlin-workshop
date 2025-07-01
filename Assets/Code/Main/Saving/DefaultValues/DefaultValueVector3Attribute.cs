using System.ComponentModel;
using UnityEngine;

namespace Awaken.TG.Main.Saving.DefaultValues {
    public class DefaultValueVector3Attribute : DefaultValueAttribute {
        public DefaultValueVector3Attribute(float x, float y, float z) : base(new Vector3(x, y, z)) { }
    }
}