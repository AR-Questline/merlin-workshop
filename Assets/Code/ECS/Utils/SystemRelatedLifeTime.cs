using System;
using System.Runtime.CompilerServices;
using Awaken.Utility.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.ECS.Utils {
    /// <summary>
    /// We can not collect spawned entities at spawn time because EntityCommandBuffer don't return real entities
    /// So we mark them with appropriate IdComponent and remove them by matching that component
    ///
    /// REQUIRES: <code>[assembly: RegisterGenericComponentType(typeof(SystemRelatedLifeTime&lt;TSystem&gt;.IdComponent))]</code>
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    public static class SystemRelatedLifeTime<TSystem> where TSystem : ISystemWithLifetime {
        static EntityQuery s_destroyQuery;
        static World s_queryWorld;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitQuery() {
            InitQuery(World.DefaultGameObjectInjectionWorld);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitQuery(World world) {
            if (s_queryWorld != null && s_queryWorld != world) {
                if (s_queryWorld.IsCreated) {
                    s_destroyQuery.Dispose();
                }
                s_destroyQuery = default;
            }
            if (s_queryWorld is { IsCreated: false } || s_destroyQuery == default) {
                var entityManager = world.EntityManager;
                s_destroyQuery = entityManager.CreateEntityQuery(typeof(IdComponent));
                s_queryWorld = world;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntities(IdComponent idComponent) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true }) {
                return;
            }
            DestroyEntities(world, idComponent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DestroyEntities(World world, IdComponent idComponent) {
            InitQuery(world);
            var entityManager = world.EntityManager;
            entityManager.GetAllUniqueSharedComponents<IdComponent>(out var idComponents, ARAlloc.Temp);
            if (idComponents.Contains(idComponent)) {
                s_destroyQuery.SetSharedComponentFilter(idComponent);
                entityManager.DestroyEntity(s_destroyQuery);
            }
            idComponents.Dispose();
        }

        public readonly struct IdComponent : ISharedComponentData, IEquatable<IdComponent> {
            public readonly int id;

            public IdComponent(int id) {
                this.id = id;
            }

            public readonly bool Equals(IdComponent other) {
                return id.Equals(other.id);
            }

            public readonly override bool Equals(object obj) {
                return obj is IdComponent other && Equals(other);
            }

            public readonly override int GetHashCode() {
                return id.GetHashCode();
            }

            public static bool operator ==(IdComponent left, IdComponent right) {
                return left.Equals(right);
            }

            public static bool operator !=(IdComponent left, IdComponent right) {
                return !left.Equals(right);
            }
        }
    }
}
