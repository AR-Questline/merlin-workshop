using System;
using Unity.Entities;

namespace Awaken.ECS.Critters.Components {
    public struct CrittersGroupEntity : ISharedComponentData, IEquatable<CrittersGroupEntity> {
        public Entity value;

        public CrittersGroupEntity(Entity value) {
            this.value = value;
        }

        public bool Equals(CrittersGroupEntity other) {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj) {
            return obj is CrittersGroupEntity other && Equals(other);
        }

        public override int GetHashCode() {
            return value.GetHashCode();
        }
    }
}