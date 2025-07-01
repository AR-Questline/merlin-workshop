using System;
using Awaken.Utility.Graphics.Mipmaps;
using Unity.Entities;

namespace Awaken.ECS.Mipmaps.Components {
    public unsafe struct MipmapsMaterialIdsComponent : ISharedComponentData, IEquatable<MipmapsMaterialIdsComponent> {
        public MipmapsStreamingMasterMaterials.MaterialId* ids;

        public bool Equals(MipmapsMaterialIdsComponent other) {
            return ids == other.ids;
        }

        public override bool Equals(object obj) {
            return obj is MipmapsMaterialIdsComponent other && Equals(other);
        }

        public override int GetHashCode() {
            return unchecked((int)(long)ids);
        }
    }
}