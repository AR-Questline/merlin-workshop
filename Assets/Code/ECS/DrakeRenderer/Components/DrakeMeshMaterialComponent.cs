using System;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Components {
    public struct DrakeMeshMaterialComponent : ICleanupComponentData, IEquatable<DrakeMeshMaterialComponent> {
        public readonly ushort meshIndex;
        public readonly ushort materialIndex;
        public readonly sbyte submesh;

        public DrakeMeshMaterialComponent(ushort meshIndex, ushort materialIndex, sbyte submesh) {
            this.meshIndex = meshIndex;
            this.materialIndex = materialIndex;
            this.submesh = submesh;
        }

        public bool Equals(DrakeMeshMaterialComponent other) {
            return meshIndex == other.meshIndex && materialIndex == other.materialIndex && submesh == other.submesh;
        }
        public override bool Equals(object obj) {
            return obj is DrakeMeshMaterialComponent other && Equals(other);
        }
        public override int GetHashCode() {
            unchecked {
                var hashCode = meshIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ materialIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ submesh.GetHashCode();
                return hashCode;
            }
        }
        public static bool operator ==(DrakeMeshMaterialComponent left, DrakeMeshMaterialComponent right) {
            return left.Equals(right);
        }
        public static bool operator !=(DrakeMeshMaterialComponent left, DrakeMeshMaterialComponent right) {
            return !left.Equals(right);
        }
    }
}
