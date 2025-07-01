using System;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Components {
    public struct LinkedTransformComponent : IComponentData, IEquatable<LinkedTransformComponent>, IWithUnityObjectRef {
        public readonly UnityObjectRef<Transform> transform;

        Type IWithUnityObjectRef.Type => typeof(Transform);
        Object IWithUnityObjectRef.Object => transform.Value;

        public LinkedTransformComponent(Transform transform) {
            this.transform = transform;
        }

        public bool Equals(LinkedTransformComponent other) {
            return transform.Equals(other.transform);
        }

        public override int GetHashCode() {
            return transform.GetHashCode();
        }
    }
}
