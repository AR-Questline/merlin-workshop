using System;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Components {
    public struct DrakeMeshMaterialComponent : ICleanupComponentData, IEquatable<DrakeMeshMaterialComponent> {
        public readonly ushort meshIndex;
        public readonly ushort materialIndex;
        public readonly sbyte submesh;

        public DrakeMeshMaterialComponent(ushort meshIndex, ushort materialIndex, sbyte submesh) {
            throw new NotImplementedException();
        }

        public bool Equals(DrakeMeshMaterialComponent other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }

        public static bool operator ==(DrakeMeshMaterialComponent left, DrakeMeshMaterialComponent right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(DrakeMeshMaterialComponent left, DrakeMeshMaterialComponent right) {
            throw new NotImplementedException();
        }
    }
}