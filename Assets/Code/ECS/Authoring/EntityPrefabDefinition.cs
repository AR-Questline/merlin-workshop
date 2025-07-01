using System;
using System.Collections.Generic;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.ECS.Authoring {
    [Serializable, Il2CppEagerStaticClassConstruction]
    public struct EntityPrefabDefinition : IEquatable<EntityPrefabDefinition> {
        public SerializableRenderMeshDescription descriptor;
        [SerializeField] bool transparent;
        [SerializeField] int guid;

        public bool IsTransparent => transparent;

        public EntityPrefabDefinition(SerializableRenderMeshDescription descriptor, bool transparent) {
            this.descriptor = descriptor;

            this.transparent = transparent;
            unchecked {
                guid = descriptor.GetHashCode();
                guid = (guid * 397) ^ transparent.GetHashCode();
            }
        }

        public EntityPrefabDefinition(Renderer renderer, bool transparent) {
            descriptor = new(renderer);

            this.transparent = transparent;
            unchecked {
                guid = descriptor.GetHashCode();
                guid = (guid * 397) ^ transparent.GetHashCode();
            }
        }

        public readonly override bool Equals(object obj) {
            return obj is EntityPrefabDefinition other && Equals(other);
        }

        public readonly bool Equals(EntityPrefabDefinition other) {
            return descriptor.Equals(other.descriptor) && transparent == other.transparent;
        }

        public readonly override int GetHashCode() {
            return guid;
        }

        public static bool operator ==(EntityPrefabDefinition left, EntityPrefabDefinition right) {
            return left.Equals(right);
        }

        public static bool operator !=(EntityPrefabDefinition left, EntityPrefabDefinition right) {
            return !left.Equals(right);
        }

        sealed class EqualityComparer : IEqualityComparer<EntityPrefabDefinition> {
            public bool Equals(EntityPrefabDefinition x, EntityPrefabDefinition y) {
                return x.Equals(y);
            }

            public int GetHashCode(EntityPrefabDefinition obj) {
                return obj.GetHashCode();
            }
        }
    }
}
