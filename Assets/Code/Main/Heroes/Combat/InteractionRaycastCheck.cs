using System;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Enums;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    [Serializable]
    public class InteractionRaycastCheck : RaycastCheck {
        public DebugInteractionCastData DebugCastData { get; private set; }
        [SerializeField] float npcFaceDistance = 1.3F;
        [SerializeField] float sphereCastRadius = .5f;
        [SerializeField] string[] interactionTags = { "InteractionObject" };
        RaycastHit[] _hits = new RaycastHit[32];
        float _maxExtensionDistance;

        public void Init() {
            _maxExtensionDistance = Mathf.Max(RichEnum.AllValuesOfType<ExtensionDistance>().Max(x => x.distance), npcFaceDistance);
        }

        protected override bool RayCheck(Vector3 origin, Vector3 direction, out HitResult hitInfo, float defaultDistance,
            QueryTriggerInteraction queryTriggerInteraction, int mask, out Result result) {
            hitInfo = default;
            result = default;
            float checkDistance = defaultDistance + _maxExtensionDistance;

            if (RaycastThroughIgnoredObjects(origin, direction, out var hit, checkDistance, queryTriggerInteraction, mask)) {
                if (CheckIfInteractionObjectOrExtension(defaultDistance, direction, hit)) {
                    hitInfo = new HitResult(hit.collider, hit.point, hit.normal);
                    result = Result.Accepted;

                    DebugCastData = new(DebugInteractionCastData.CastType.Ray, origin, direction, hit.distance, hit: true);

                    return true;
                }

                if (hit.distance <= defaultDistance) {
                    defaultDistance = hit.distance;
                }
            }

            return RayCheckFallback(origin, direction, defaultDistance, mask, ref hitInfo, ref result);
        }

        bool RaycastThroughIgnoredObjects(Vector3 origin, Vector3 direction, out RaycastHit resultHit, float distance, 
            QueryTriggerInteraction queryTriggerInteraction, int mask) {
            
            int hitCount = Physics.RaycastNonAlloc(origin, direction, _hits, distance, mask, queryTriggerInteraction);

            bool hasClosestHit = false;
            RaycastHit closestHit = default;
                
            for (int i = 0; i < hitCount; i++) {
                var hit = _hits[i];
                    
                if(!hasClosestHit || hit.distance < closestHit.distance) {
                    if (hit.collider.TryGetComponentInParent(out CanInteractThrough _)) {
                        continue;
                    }
                    
                    closestHit = hit;
                    hasClosestHit = true;
                }
            }
            
            resultHit = closestHit;
            return hasClosestHit;
        }

        bool RayCheckFallback(Vector3 origin, Vector3 direction, float defaultDistance, int mask, ref HitResult hitInfo, ref Result result) {
            var sphereCastOrigin = origin + direction.normalized * sphereCastRadius;
            defaultDistance = Mathf.Max(defaultDistance - sphereCastRadius, RaycastCheck.MinPhysicsCastDistance);
            int size = Physics.SphereCastNonAlloc(sphereCastOrigin, sphereCastRadius, direction, _hits, defaultDistance, mask);

            DebugCastData = new(DebugInteractionCastData.CastType.Sphere, sphereCastOrigin, direction, defaultDistance, sphereCastRadius);

            if (size <= 0) {
                return false;
            }

            float lowestAngle = float.MaxValue;
            bool found = false;
            RaycastHit closestHit = default;
            for (int i = 0; i < size; i++) {
                var hit = _hits[i];
                if (!CheckInteractionTagAndLayer(hit)) {
                    continue;
                }
                bool obstacleRaycast = ObstacleCheck(origin, hit.collider);
                if (!obstacleRaycast) {
                    float angle = Vector3.Angle(direction, hit.transform.position - origin);
                    if (angle < lowestAngle) {
                        found = true;
                        lowestAngle = angle;
                        closestHit = hit;
                    }
                }
            }

            if (!found) {
                DebugCastData = new(DebugInteractionCastData.CastType.Sphere,
                    sphereCastOrigin, direction, defaultDistance, sphereCastRadius, false);

                result = Result.Prevented;
                return false;
            }

            DebugCastData = new(DebugInteractionCastData.CastType.Sphere,
                sphereCastOrigin, direction, defaultDistance, sphereCastRadius, true);

            hitInfo = new HitResult(closestHit.collider, closestHit.point, closestHit.normal);
            result = Result.Accepted;
            return true;
        }

        bool ObstacleCheck(in Vector3 origin, Collider hitCollider) {
            Vector3 collisionPoint = hitCollider.ClosestPoint(origin);
            Vector3 dir = collisionPoint - origin;

            bool obstacleCheck = RaycastThroughIgnoredObjects(origin, dir.normalized, out var hit, dir.magnitude,
                QueryTriggerInteraction.Ignore, prevent);
            return obstacleCheck && hit.collider != hitCollider;
        }

        bool CheckInteractionTagAndLayer(RaycastHit hit) {
            return interactionTags.Any(hit.collider.CompareTag) || accept.Contains(hit.collider.gameObject.layer);
        }

        bool CheckIfInteractionObjectOrExtension(in float defaultDistance, in Vector3 direction, in RaycastHit hit) {
            bool isExtensionDistance = false;
            if (hit.distance > defaultDistance) {
                if (hit.collider.TryGetComponent(out InteractionObject interactionObject) && interactionObject.extendRaycast) {
                    isExtensionDistance = hit.distance < defaultDistance + interactionObject.ExtensionDistance.distance;
                    isExtensionDistance = isExtensionDistance && CheckExtensionSides(direction, hit, interactionObject);
                } else if (hit.distance <= defaultDistance + npcFaceDistance) {
                    isExtensionDistance = CheckFacingNpc(direction, hit);
                }
            }

            return (CheckInteractionTagAndLayer(hit) && hit.distance <= defaultDistance) || isExtensionDistance;
        }

        static bool CheckExtensionSides(in Vector3 direction, in RaycastHit hit, InteractionObject interactionObject) =>
            interactionObject.side switch {
                InteractionObject.ExtensionSide.Back => Vector3.Dot(hit.transform.forward, direction) > 0,
                InteractionObject.ExtensionSide.Front => Vector3.Dot(hit.transform.forward, direction) < 0,
                _ => true
            };

        static bool CheckFacingNpc(Vector3 direction, RaycastHit hit) {
            bool isExtensionDistance = false;
            try {
                NpcElement npc = VGUtils.GetModel<NpcElement>(hit.collider.gameObject);
                if (npc != null) {
                    isExtensionDistance = Vector3.Dot(npc.ParentTransform.forward, direction) < 0;
                }
            } catch {
                // ignored
            }

            return isExtensionDistance;
        }

        public struct DebugInteractionCastData {
            public enum CastType {
                Ray,
                Sphere
            }

            public readonly CastType castType;
            public readonly Vector3 origin;
            public readonly Vector3 direction;
            public readonly float distance;
            public readonly float radius;
            public readonly bool? hit;

            public DebugInteractionCastData(CastType castType, Vector3 origin, Vector3 direction, float distance,
                float radius = 0, bool? hit = null) {
                this.castType = castType;
                this.origin = origin;
                this.direction = direction;
                this.distance = distance;
                this.radius = radius;
                this.hit = hit;
            }
        }
    }
}