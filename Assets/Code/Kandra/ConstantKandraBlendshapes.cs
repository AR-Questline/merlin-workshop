using System;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.Kandra {
    public class ConstantKandraBlendshapes : MonoBehaviour {
        public ConstantBlendshape[] blendshapes;

        public void Validate(KandraRenderer renderer) {
            int count = renderer.BlendshapesCount;
            for (int i = blendshapes.Length - 1; i >= 0; i--) {
                if (blendshapes[i].index >= count) {
                    ArrayUtils.RemoveAt(ref blendshapes, i);
                }
            }
        }
        
        [Serializable]
        public struct ConstantBlendshape : IEquatable<ConstantBlendshape> {
            public ushort index;
            public float value;
            
            public bool Equals(ConstantBlendshape other) {
                return other.index == index & other.value == value;
            }
        }
    }
}