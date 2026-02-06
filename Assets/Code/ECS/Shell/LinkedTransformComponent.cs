using System;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Components {
    public struct LinkedTransformComponent : IComponentData, IEquatable<LinkedTransformComponent>, IWithUnityObjectRef {
        public readonly UnityObjectRef<Transform> transform;

        Type IWithUnityObjectRef.Type => throw new NotImplementedException();
        Object IWithUnityObjectRef.Object => throw new NotImplementedException();

        public LinkedTransformComponent(Transform transform) {
            throw new NotImplementedException();
        }

        public bool Equals(LinkedTransformComponent other) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }
    }
}