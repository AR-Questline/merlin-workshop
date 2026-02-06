using System;
using UnityEngine;

namespace Awaken.Kandra {
    public class ConstantKandraBlendshapes : MonoBehaviour {
        public ConstantBlendshape[] blendshapes = Array.Empty<ConstantBlendshape>();

        public void Validate(KandraRenderer renderer) {
            throw new NotImplementedException();
        }

        public struct ConstantBlendshape : IEquatable<ConstantBlendshape> {
            public ushort index;
            public float value;

            public bool Equals(ConstantBlendshape other) {
                throw new NotImplementedException();
            }
        }
    }
}