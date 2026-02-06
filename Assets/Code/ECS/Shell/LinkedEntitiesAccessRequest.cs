using System;
using Awaken.ECS.Authoring.LinkedEntities;
using Unity.Entities;

namespace Awaken.ECS.Components {
    public struct LinkedEntitiesAccessRequest : IComponentData, IEquatable<LinkedEntitiesAccessRequest> {
        public readonly UnityObjectRef<LinkedEntitiesAccess> linkedEntitiesAccessRef;
        public readonly bool destroyIfLinkInvalid;

        public LinkedEntitiesAccessRequest(LinkedEntitiesAccess linkedEntitiesAccessRef, bool destroyIfLinkInvalid) {
            throw new NotImplementedException();
        }

        public LinkedEntitiesAccessRequest(LinkedEntityLifetime linkedEntityLifetime) {
            throw new NotImplementedException();
        }

        public bool Equals(LinkedEntitiesAccessRequest other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }

        public static bool operator ==(LinkedEntitiesAccessRequest left, LinkedEntitiesAccessRequest right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(LinkedEntitiesAccessRequest left, LinkedEntitiesAccessRequest right) {
            throw new NotImplementedException();
        }
    }
}