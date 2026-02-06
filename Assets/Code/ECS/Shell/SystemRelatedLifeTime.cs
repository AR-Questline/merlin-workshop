using System;
using Unity.Entities;

namespace Awaken.ECS.Utils {
    public static class SystemRelatedLifeTime<TSystem> where TSystem : ISystemWithLifetime {
        public static void InitQuery() {
            throw new NotImplementedException();
        }

        public static void InitQuery(World world) {
            throw new NotImplementedException();
        }

        public static void DestroyEntities(IdComponent idComponent) {
            throw new NotImplementedException();
        }

        public readonly struct IdComponent : ISharedComponentData, IEquatable<IdComponent> {
            public readonly int id;

            public IdComponent(int id) {
                throw new NotImplementedException();
            }

            public bool Equals(IdComponent other) {
                throw new NotImplementedException();
            }

            public override bool Equals(object obj) {
                throw new NotImplementedException();
            }

            public override int GetHashCode() {
                throw new NotImplementedException();
            }

            public static bool operator ==(IdComponent left, IdComponent right) {
                throw new NotImplementedException();
            }

            public static bool operator !=(IdComponent left, IdComponent right) {
                throw new NotImplementedException();
            }
        }
    }
}