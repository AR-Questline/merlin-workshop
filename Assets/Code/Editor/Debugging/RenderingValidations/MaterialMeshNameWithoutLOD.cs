using System;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    [System.Serializable]
    public struct MaterialMeshNameWithoutLOD : IEquatable<MaterialMeshNameWithoutLOD> {
        public Material material;
        public string meshNameWithoutLOD;

        public MaterialMeshNameWithoutLOD(Material material, string meshNameWithoutLOD) {
            this.material = material;
            this.meshNameWithoutLOD = meshNameWithoutLOD;
        }

        public bool Equals(MaterialMeshNameWithoutLOD other) {
            return Equals(material.GetHashCode(), other.material.GetHashCode()) && meshNameWithoutLOD == other.meshNameWithoutLOD;
        }

        public override bool Equals(object obj) {
            return obj is MaterialMeshNameWithoutLOD other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(material.GetHashCode(), meshNameWithoutLOD.GetHashCode());
        }
    }
}