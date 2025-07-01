using System;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.Utils;
using Awaken.Utility.Debugging;
using Unity.Entities;

namespace Awaken.ECS.Components {
    public struct LinkedEntitiesAccessRequest : IComponentData, IEquatable<LinkedEntitiesAccessRequest> {
        public readonly UnityObjectRef<LinkedEntitiesAccess> linkedEntitiesAccessRef;
        public readonly bool destroyIfLinkInvalid;

        public LinkedEntitiesAccessRequest(LinkedEntitiesAccess linkedEntitiesAccessRef, bool destroyIfLinkInvalid) {
            this.linkedEntitiesAccessRef = linkedEntitiesAccessRef;
            this.destroyIfLinkInvalid = destroyIfLinkInvalid;
        }
        
        public LinkedEntitiesAccessRequest(LinkedEntityLifetime linkedEntityLifetime) {
            var linkedEntitiesAccess = linkedEntityLifetime.linkedEntitiesAccess;
            if (linkedEntitiesAccess == null) {
                Log.Important?.Error($"{nameof(LinkedEntitiesAccess)} is null. Forgot to call LinkedEntityLifetime.Init() or added LinkedEntityLifetime not in runtime");
            }
            this.linkedEntitiesAccessRef = linkedEntitiesAccess;
            this.destroyIfLinkInvalid = true;
        }

        public bool Equals(LinkedEntitiesAccessRequest other) {
            return linkedEntitiesAccessRef.Equals(other.linkedEntitiesAccessRef);
        }
        public override bool Equals(object obj) {
            return obj is LinkedEntitiesAccessRequest other && Equals(other);
        }
        public override int GetHashCode() {
            return linkedEntitiesAccessRef.GetHashCode();
        }
        public static bool operator ==(LinkedEntitiesAccessRequest left, LinkedEntitiesAccessRequest right) {
            return left.Equals(right);
        }
        public static bool operator !=(LinkedEntitiesAccessRequest left, LinkedEntitiesAccessRequest right) {
            return !left.Equals(right);
        }
    }
}
