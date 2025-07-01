using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Awaken.Utility.PhysicUtils;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    [Serializable]
    public class RaycastCheck {
        public const int MultiselectArraySize = 100;
        public const float MinPhysicsCastDistance = 0.0001f;
        const float AdditionalRaycastDistanceLose = 0.05f;

        public LayerMask accept;
        public LayerMask prevent;
        
        public Result Check(Vector3 origin, Vector3 direction, out HitResult hitInfo, float maxDistance, float sphereSize = 0.02f, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            int mask = accept | prevent;
            using var rentedArray = RentedArray<Collider>.Borrow(1);
            if (sphereSize > 0 && Physics.OverlapSphereNonAlloc(origin, sphereSize, rentedArray.GetBackingArray(), mask, queryTriggerInteraction) > 0) {
                if (RayCheck(origin, rentedArray[0].ClosestPoint(origin) - origin, out hitInfo, sphereSize, queryTriggerInteraction, mask, out Result sphereCastToRaycastResult)) {
                    return sphereCastToRaycastResult;
                }
            }

            if (RayCheck(origin, direction, out hitInfo, maxDistance, queryTriggerInteraction, mask, out Result result)) {
                return result;
            }

            hitInfo = new HitResult();
            return Result.Ignored;
        }
        
        public Result CheckMultiHit(Vector3 origin, Vector3 direction, out List<HitResult> hitInfos, float maxDistance, float sphereSize = 0.02f, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            int mask = accept | prevent;
            using var rentedArray = RentedArray<Collider>.Borrow(MultiselectArraySize);
            hitInfos = new List<HitResult>();
            Vector3 currOrigin = origin;
            float currMaxDistance = maxDistance;
            bool prevented = false;
            if (sphereSize > 0) {
                int hitCount = Physics.OverlapSphereNonAlloc(origin, sphereSize, rentedArray.GetBackingArray(), mask, queryTriggerInteraction); 
                var preventedHitInfos = new List<HitResult>();
                for (int i = 0; i < hitCount; i++) {
                    if (RayCheck(origin, rentedArray[0].ClosestPoint(origin) - origin, out var hitInfo, sphereSize, queryTriggerInteraction, mask, out Result sphereCastToRaycastResult)) {
                        currMaxDistance -= (hitInfo.Point - currOrigin).magnitude + AdditionalRaycastDistanceLose;
                        currOrigin = hitInfo.Point + direction * AdditionalRaycastDistanceLose;
                        if (sphereCastToRaycastResult == Result.Prevented) {
                            preventedHitInfos.Add(hitInfo);
                            prevented = true;
                        } else {
                            hitInfos.Add(hitInfo);
                        }
                    }
                }
                // Prevented hits should be added to the end of hitInfos to return Result.Accepted at the end.
                hitInfos.AddRange(preventedHitInfos);
            }

            if (!prevented) {
                while (currMaxDistance > 0 && RayCheck(currOrigin, direction, out var hitInfo, currMaxDistance, queryTriggerInteraction, mask, out Result result)) {
                    hitInfos.Add(hitInfo);
                    if (result == Result.Prevented) {
                        break;
                    }

                    currMaxDistance -= (hitInfo.Point - currOrigin).magnitude + AdditionalRaycastDistanceLose;
                    currOrigin = hitInfo.Point + direction * AdditionalRaycastDistanceLose;
                }
            }

            if (hitInfos.Count > 0) {
                return prevent.Contains(hitInfos[0].Collider.gameObject.layer) ? Result.Prevented : Result.Accepted;
            }
            return Result.Ignored;
        }

        protected virtual bool RayCheck(Vector3 origin, Vector3 direction, out HitResult hitInfo, float defaultDistance,
            QueryTriggerInteraction queryTriggerInteraction, int mask, out Result result) {
            hitInfo = default;
            result = default;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, defaultDistance, mask, queryTriggerInteraction)) {
                bool prevented = prevent.Contains(hit.collider.gameObject.layer);
                hitInfo = new HitResult(hit.collider, hit.point, hit.normal, prevented);
                result = prevented ? Result.Prevented : Result.Accepted;
                return true;
            }

            return false;
        }

        public Collider Detected(Vector3 origin, Vector3 direction, float maxDistance, float sphereSize = 0.02f, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            return Check(origin, direction, out var hitResult, maxDistance, sphereSize, queryTriggerInteraction) switch {
                Result.Accepted => hitResult.Collider,
                _ => null
            };
        }
        
        public HitResult Raycast(Vector3 origin, Vector3 direction, float maxDistance, float sphereSize = 0.02f, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            return Check(origin, direction, out HitResult hitResult, maxDistance, sphereSize, queryTriggerInteraction) switch {
                Result.Accepted => hitResult,
                Result.Prevented => hitResult,
                _ => new HitResult()
            };
        }
        
        public List<HitResult> RaycastMultiHit(Vector3 origin, Vector3 direction, float maxDistance, float sphereSize = 0.02f, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            return CheckMultiHit(origin, direction, out List<HitResult> hitResults, maxDistance, sphereSize, queryTriggerInteraction) switch {
                Result.Accepted => hitResults,
                Result.Prevented => hitResults,
                _ => new List<HitResult>()
            };
        }

        public PhysicsQueries.OverlapCapsuleQuery OverlapTargetsCapsule(Vector3 origin, Vector3 velocity, float deltaTime, float ellipse, float width,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            var end = origin + velocity.normalized*width + velocity*deltaTime;
            return PhysicsQueries.OverlapCapsule(origin, end, ellipse, accept, queryTriggerInteraction);
        }
        
        public PhysicsQueries.OverlapBoxQuery OverlapTargetsBox(Vector3 origin, Vector3 direction, float maxDistance, Vector2 boxCastSize, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            int mask = accept | prevent;
            Vector3 halfExtents = new (boxCastSize.x / 2, boxCastSize.y / 2, maxDistance / 2);
            Vector3 boxCastCenter = origin + direction.normalized * halfExtents.z;
            return PhysicsQueries.OverlapBox(boxCastCenter, halfExtents, mask, queryTriggerInteraction);
        }

        public enum Result {
            Ignored,
            Prevented,
            Accepted,
        }
    }
}