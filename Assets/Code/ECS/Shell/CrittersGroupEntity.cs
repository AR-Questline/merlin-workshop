using System;
using Unity.Entities;

namespace Awaken.ECS.Critters.Components {
    public struct CrittersGroupEntity : ISharedComponentData, IEquatable<CrittersGroupEntity> {
        public Entity value;

        public CrittersGroupEntity(Entity value) {
            throw new NotImplementedException();
        }

        public bool Equals(CrittersGroupEntity other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }
    }
}