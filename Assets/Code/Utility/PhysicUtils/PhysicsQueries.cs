using System;
using System.Collections.Generic;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.PhysicUtils {
    public static class PhysicsQueries {
        const int MaxOverlappingColliders = 128;
        static readonly Stack<Collider[]> CollidersStack = new();
        
        public static OverlapSphereQuery OverlapSphere(Vector3 position, float radius, int mask = -1, QueryTriggerInteraction interaction = QueryTriggerInteraction.UseGlobal) {
            return new(position, radius, mask, interaction);
        }
        
        public static OverlapConeApproxQuery OverlapConeApprox(Vector3 position, float radius, float angle, Vector3 forward, int mask = -1, QueryTriggerInteraction interaction = QueryTriggerInteraction.UseGlobal) {
            return new(position, radius, angle, forward, mask, interaction);
        }

        public static OverlapCapsuleQuery OverlapCapsule(Vector3 point1, Vector3 point2, float radius, int mask = -1, QueryTriggerInteraction interaction = QueryTriggerInteraction.UseGlobal) {
            return new(point1, point2, radius, mask, interaction);
        }

        public static OverlapBoxQuery OverlapBox(Vector3 center, Vector3 halfExtends, int mask = -1, QueryTriggerInteraction interaction = QueryTriggerInteraction.UseGlobal) {
            return new(center, halfExtends, Quaternion.identity, mask, interaction);
        }
        
        public static OverlapBoxQuery OverlapBox(Vector3 center, Vector3 halfExtends, Quaternion rotation, int mask = -1, QueryTriggerInteraction interaction = QueryTriggerInteraction.UseGlobal) {
            return new(center, halfExtends, rotation, mask, interaction);
        }
        
        static Collider[] GetColliders() {
            return CollidersStack.TryPop(out var colliders) ? colliders : new Collider[MaxOverlappingColliders];
        }
        
        public readonly ref struct OverlapSphereQuery {
            public readonly Vector3 position;
            public readonly float radius;
            public readonly int mask;
            public readonly QueryTriggerInteraction interaction;
            
            public OverlapSphereQuery(Vector3 position, float radius, int mask, QueryTriggerInteraction interaction) {
                this.position = position;
                this.radius = radius;
                this.mask = mask;
                this.interaction = interaction;
            }

            public UniqueIterator GetEnumerator() {
                var colliders = GetColliders();
                var length = Physics.OverlapSphereNonAlloc(position, radius, colliders, mask, interaction);
                return new UniqueIterator(colliders, length);
            }
        }
        
        public readonly ref struct OverlapConeApproxQuery {
            public readonly Vector3 position;
            public readonly Vector3 forward;
            public readonly float radius;
            public readonly float validCos;
            public readonly int mask;
            public readonly QueryTriggerInteraction interaction;
            
            public OverlapConeApproxQuery(Vector3 position, float radius, float angle, Vector3 forward, int mask, QueryTriggerInteraction interaction) {
                this.position = position;
                this.forward = forward;
                this.radius = radius;
                this.validCos = math.cos(angle * Mathf.Deg2Rad);
                this.mask = mask;
                this.interaction = interaction;
            }

            public UniqueIterator GetEnumerator() {
                var colliders = GetColliders();
                var length = Physics.OverlapSphereNonAlloc(position, radius, colliders, mask, interaction);
                length = RemoveOutsideConeAtSwapBack(colliders, length);
                return new UniqueIterator(colliders, length);
            }

            int RemoveOutsideConeAtSwapBack(Collider[] colliders, int length) {
                for (int i = length - 1; i >= 0 ; i--) {
                    var direction = (GetClosestPointOnCollider(colliders[i], position, forward) - position).normalized;
                    if (Vector3.Dot(direction, forward) < validCos) {
                        colliders[i] = colliders[length - 1];
                        length--;
                    }
                }
                return length;
            }

            static Vector3 GetClosestPointOnCollider(Collider collider, Vector3 lineStart, Vector3 lineForward) {
                // This function may be inaccurate in some edge cases, but it's good enough for our purposes.
                return collider.ClosestPoint(collider.ProjectCenterOntoLine(lineStart, lineForward));
            }
        }

        public readonly ref struct OverlapCapsuleQuery {
            public readonly Vector3 point1;
            public readonly Vector3 point2;
            public readonly float radius;
            public readonly int mask;
            public readonly QueryTriggerInteraction interaction;

            public OverlapCapsuleQuery(Vector3 point1, Vector3 point2, float radius, int mask, QueryTriggerInteraction interaction) {
                this.point1 = point1;
                this.point2 = point2;
                this.radius = radius;
                this.mask = mask;
                this.interaction = interaction;
            }

            public UniqueIterator GetEnumerator() {
                var colliders = GetColliders();
                var length = Physics.OverlapCapsuleNonAlloc(point1, point2, radius, colliders, mask, interaction);
                return new UniqueIterator(colliders, length);
            }
        }

        public readonly ref struct OverlapBoxQuery {
            public readonly Vector3 center;
            public readonly Vector3 halfExtends;
            public readonly Quaternion rotation;
            public readonly int mask;
            public readonly QueryTriggerInteraction interaction;

            public OverlapBoxQuery(Vector3 center, Vector3 halfExtends, Quaternion rotation, int mask, QueryTriggerInteraction interaction) {
                this.center = center;
                this.halfExtends = halfExtends;
                this.rotation = rotation;
                this.mask = mask;
                this.interaction = interaction;
            }

            public UniqueIterator GetEnumerator() {
                var colliders = GetColliders();
                var length = Physics.OverlapBoxNonAlloc(center, halfExtends, colliders, rotation, mask, interaction);
                return new UniqueIterator(colliders, length);
            }
        }
        
        public ref struct UniqueIterator {
            readonly Collider[] _colliders;
            readonly int _length;
            int _index;

            public UniqueIterator(Collider[] colliders, int length) {
#if UNITY_EDITOR
                if (length == MaxOverlappingColliders) {
                    Debugging.Log.Important?.Error("Physics query returned more than " + MaxOverlappingColliders + " colliders. This is not supported.");
                }
#endif
                _colliders = colliders;
                _length = length;
                _index = -1;
            }
            
            public bool MoveNext() {
                return ++_index < _length;
            }

            public Collider Current => _colliders[_index];

            [UnityEngine.Scripting.Preserve]
            public void Dispose() {
                Array.Clear(_colliders, 0, _length);
                CollidersStack.Push(_colliders);
            }
        }
    }
}