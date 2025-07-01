using System.Linq;
using Awaken.TG.Utility.Maths;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Utility {
    public static class TransformBoundsUtil {
        /// <summary>
        /// Calculate bounds of reference and children
        /// </summary>
        /// <param name="reference">Start point for calculation</param>
        /// <param name="excluded">Name parts of excluded bounds contributors</param>
        /// <param name="includeInactive">Include inactive contributors in calculations</param>
        /// <returns>World space bounds</returns>
        public static Bounds FindBounds(Transform reference, bool includeInactive, params string[] excluded) {
            Bounds bounds = new Bounds();
            // Please note that bounds cannot be taken from SkinnedMeshRenderer, so it's not included in these calculations
            var meshFilters = reference.GetComponentsInChildren<MeshFilter>(includeInactive);
            foreach (var filter in meshFilters) {
                if (excluded.Contains(filter.name) || filter.GetComponent<IgnoreBounds>() != null) {
                    continue;
                }
                
                if (filter.sharedMesh == null) {
                    Log.Important?.Error($"Null mesh in location: {reference.gameObject.name} in position {reference.position}");
                    continue;
                }
                var localToWorld = filter.transform.localToWorldMatrix;
                var meshBounds = filter.sharedMesh.bounds;
                var meshWorldBounds = new Bounds();
                meshWorldBounds.center = localToWorld.MultiplyPoint(meshBounds.center);
                meshWorldBounds.extents = localToWorld.MultiplyVector(meshBounds.extents).Abs();
                if (bounds.extents != Vector3.zero) {
                    bounds.Encapsulate(meshWorldBounds);
                } else {
                    bounds = meshWorldBounds;
                }
            }

            if (bounds.extents == Vector3.zero) {
                // get bounds from colliders
                bounds = FindBoundsFromCollidersOnly(reference, includeInactive, excluded);
            }
            
            return bounds;
        }

        public static Bounds FindBoundsFromCollidersOnly(Transform reference, bool includeInactive, params string[] excluded) {
            Bounds bounds = new Bounds();
            var colliders = reference.GetComponentsInChildren<Collider>(includeInactive);
            foreach (var collider in colliders) {
                if (excluded.Contains(collider.name) || collider.GetComponent<IgnoreBounds>() != null) {
                    continue;
                }

                var localToWorld = collider.transform.localToWorldMatrix;
                var colliderBounds = new Bounds();
                if (collider is BoxCollider boxCollider) {
                    colliderBounds.center = localToWorld.MultiplyPoint(boxCollider.center);
                    colliderBounds.extents = localToWorld.MultiplyVector(boxCollider.size * 0.5f).Abs();
                } else if (collider is SphereCollider sphereCollider) {
                    colliderBounds.center = localToWorld.MultiplyPoint(sphereCollider.center);
                    colliderBounds.extents = localToWorld.MultiplyVector(Vector3.one * sphereCollider.radius).Abs();
                } else if (collider is CapsuleCollider capsuleCollider) {
                    Vector3 mainDimension = CapsuleMainDimension(capsuleCollider) * capsuleCollider.height;
                    Vector3 secondaryDimensions = CapsuleSecondaryDimensions(capsuleCollider) * capsuleCollider.radius;
                    colliderBounds.center = localToWorld.MultiplyPoint(capsuleCollider.center);
                    colliderBounds.extents = localToWorld.MultiplyVector(mainDimension * 0.5f + secondaryDimensions).Abs();
                } else {
                    // fallback but collider.bounds works only on active instance
                    colliderBounds = collider.bounds;
                }

                if (bounds.extents != Vector3.zero) {
                    bounds.Encapsulate(colliderBounds);
                } else {
                    bounds = colliderBounds;
                }
            }

            return bounds;
        }

        public static void AdjustColliderByBounds<T>(T collider, Bounds bounds, float sizeFactor = 1f) where T : Collider {
            bounds.size *= sizeFactor;
            if (collider is BoxCollider boxCollider) {
                boxCollider.size = bounds.size;
                boxCollider.center = bounds.center;
            } else if (collider is SphereCollider sphereCollider) {
                float radius = (bounds.extents[0] + bounds.extents[1] + bounds.extents[2]) / 3f;
                sphereCollider.radius = radius;
                sphereCollider.center = bounds.center;
            } else if (collider is CapsuleCollider capsuleCollider) {
                float[] array = {bounds.size.x, bounds.size.y, bounds.size.z};
                int direction = array.IndexOf(array.Max());
                int nextDimension = (direction + 1) % 3;
                int lastDimension = (direction + 2) % 3;
                Vector3 directionVector = Vector3.zero;
                directionVector[direction] = 1f;

                capsuleCollider.direction = direction;

                float radius = (bounds.extents[nextDimension] + bounds.extents[lastDimension]) / 2f;
                capsuleCollider.radius = radius;
                capsuleCollider.height = bounds.size[direction];
                capsuleCollider.center = bounds.center;
            }
        }

        static Vector3 CapsuleMainDimension(CapsuleCollider capsuleCollider) {
            if (capsuleCollider.direction == 0) {
                return new Vector3(1, 0, 0);
            }
            if (capsuleCollider.direction == 1) {
                return new Vector3(0, 1, 0);
            }

            return new Vector3(0, 0, 1);
        }
        
        static Vector3 CapsuleSecondaryDimensions(CapsuleCollider capsuleCollider) {
            if (capsuleCollider.direction == 0) {
                return new Vector3(0, 1, 1);
            }
            if (capsuleCollider.direction == 1) {
                return new Vector3(1, 0, 1);
            }

            return new Vector3(1, 1, 0);
        }
    }
}