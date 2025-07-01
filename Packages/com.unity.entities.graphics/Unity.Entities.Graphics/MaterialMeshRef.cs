#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Unity.Rendering {
    [Serializable]
    public struct MaterialMeshRef : IEquatable<MaterialMeshRef> {
        public Material material;
        public Mesh mesh;
        public MaterialMeshRef(Material material, Mesh mesh) {
            this.material = material;
            this.mesh = mesh;
        }
        
        public bool Equals(MaterialMeshRef other) {
            return Equals(material.GetHashCode(), other.material.GetHashCode()) && Equals(mesh.GetHashCode(), other.mesh.GetHashCode());
        }

        public override bool Equals(object obj) {
            return obj is MaterialMeshRef other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(material.GetHashCode(), mesh.GetHashCode());
        }
    }
}
#endif