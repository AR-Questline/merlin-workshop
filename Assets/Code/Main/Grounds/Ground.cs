using Awaken.TG.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniversalProfiling;

namespace Awaken.TG.Main.Grounds {
    /// <summary>
    /// Performs raycasts to check height of terrain at given 2D point
    /// </summary>
    public static class Ground {
        public const float MaxRayHeight = 1000f;
        public const float BelowGroundHeight = 300f;
        public const int LayerMask = RenderLayers.Mask.Walkable | RenderLayers.Mask.Terrain;
        public const int NpcGroundLayerMask = RenderLayers.Mask.CharacterGround;

        // === Queries
        public static Vector3 CoordsToWorld(Vector3 coords, int raycastMask = LayerMask) {
            Vector3 v = coords;
            v.y = HeightAt(coords, raycastMask, true);
            return v;
        }

        public static bool Raycast(Ray ray, out Vector3 position, out Vector3 normal, float castDistance = Mathf.Infinity, int raycastMask = LayerMask, PhysicsScene physicsScene = default, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            if (!physicsScene.IsValid()) {
                physicsScene = Physics.defaultPhysicsScene;
            }
            // do an actual raycast
            RaycastHit hit;
            bool foundSomething = physicsScene.Raycast(ray.origin, ray.direction, out hit, castDistance, raycastMask, queryTriggerInteraction);
            // translate to our output style
            position = foundSomething ? hit.point : Vector3.zero;
            normal = foundSomething ? hit.normal : Vector3.up;
            return foundSomething;
        }

        /// <summary>
        /// Places the transform on the ground, while making sure it's not below the terrain.
        /// Returns true if the transform was changed.
        /// </summary>
        public static bool SnapToGroundSafe(Transform transform, AlignMode align = AlignMode.None, PhysicsScene physicsScene = default) {
            bool snapped = false;
            bool isOpenWorld = false;
            for (int i = 0; !isOpenWorld & i < SceneManager.sceneCount; i++) {
                isOpenWorld |= CommonReferences.Get.SceneConfigs.IsOpenWorld(SceneReference.ByScene(SceneManager.GetSceneAt(i)));
            }
            
            if (!physicsScene.IsValid()) {
                physicsScene = Physics.defaultPhysicsScene;
            }

            Vector3 position = isOpenWorld
                ? Ground.FindClosestNotBelowTerrain(transform.position, transform, physicsScene)
                : Ground.SnapToGround(transform.position, transform, physicsScene: physicsScene);
            if (math.abs(position.y - transform.position.y) > 0.001f) {
                transform.position = position;
                snapped = true;
            }

            Vector3 eulerAngles = transform.eulerAngles;
            if (align == AlignMode.GroundNormal) {
                physicsScene.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 100f, LayerMask);
                Vector3 hitNormal = hit.normal;
                RotateToUp(transform, hitNormal);
            } else if (align == AlignMode.Up) {
                RotateToUp(transform, Vector3.up);
            }

            if (eulerAngles != transform.eulerAngles) {
                snapped = true;
            }

            return snapped;

            static void RotateToUp(Transform transform, Vector3 up) {
                var rotateAbout = Quaternion.FromToRotation(transform.up, up);
                transform.rotation = rotateAbout * transform.rotation;
            }
        }

        public static Vector3 SnapNpcToGround(Vector3 position) {
            position.y = HeightAt(position, NpcGroundLayerMask, true);
            return position;
        }

        public static Vector3 SnapToGround(Vector3 position, Transform ignoreRoot = null, bool findClosest = true, PhysicsScene physicsScene = default) {
            position.y = HeightAt(position, findClosest: findClosest, ignoreRoot: ignoreRoot, physicsScene: physicsScene);
            return position;
        }

        public static Vector3 FindClosestNotBelowTerrain(Vector3 position, Transform ignoreRoot = null, PhysicsScene physicsScene = default) {
            position.y = FindClosestNotBelowTerrain(position, LayerMask, ignoreRoot, physicsScene);
            return position;
        }

        public static float HeightAt(Vector3 coords, int raycastMask = LayerMask, bool findClosest = false, Transform ignoreRoot = null,
            bool performExtraChecks = false, PhysicsScene physicsScene = default, float rayStartY = MaxRayHeight, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            return HeightAndNormalAt(coords, raycastMask, findClosest, ignoreRoot, performExtraChecks, physicsScene, rayStartY, queryTriggerInteraction).height;
        }
        
        public static (float height, Vector3 normal) HeightAndNormalAt(Vector3 coords, int raycastMask = LayerMask, bool findClosest = false, Transform ignoreRoot = null, bool performExtraChecks = false, PhysicsScene physicsScene = default, float rayStartY = MaxRayHeight, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            if (findClosest) {
                return FindClosest(coords, raycastMask, ignoreRoot, performExtraChecks, physicsScene, queryTriggerInteraction);
            }

            return TryHit(coords, raycastMask, rayStartY, rayStartY + BelowGroundHeight, out Vector3 hitPos, out Vector3 hitNormal, physicsScene, queryTriggerInteraction) ?
                (hitPos.y, hitNormal) : (0, Vector3.up);
        }

        public static void Snap2DPointsToGround(in UnsafeArray<float2> points, float originHeight, QueryParameters parameters, Allocator allocator,
            out UnsafeArray<float3> worldPoints) {
            // Setup the command and result buffers
            var results = new NativeArray<RaycastHit>((int)points.Length, ARAlloc.TempJob);
            var commands = new NativeArray<RaycastCommand>((int)points.Length, ARAlloc.TempJob);
            worldPoints = new UnsafeArray<float3>(points.Length, allocator);

            Vector3 direction = Vector3.down;

            for (var i = 0u; i < commands.Length; i++) {
                commands[(int)i] = new RaycastCommand(new Vector3(points[i].x, originHeight, points[i].y), direction, parameters);
            }

            // Run the batch of raycasts.
            RaycastCommand.ScheduleBatch(commands, results, 1, 1).Complete();

            // Copy the result. If batchedHit.collider is null there was no hit
            for (var index = 0u; index < results.Length; index++) {
                RaycastHit hit = results[(int)index];
                if (hit.collider != null) {
                    worldPoints[index] = hit.point;
                } else {
                    worldPoints[index] = points[index].x0y();
                }
            }

            // Dispose the buffers
            results.Dispose();
            commands.Dispose();
        }

        public static bool TryHit(Vector3 coords, int raycastMask, float yCoord, float castDistance, out Vector3 hitPos, out Vector3 hitNormal, PhysicsScene physicsScene,
            QueryTriggerInteraction queryTriggerInteraction) {
            Vector3 origin = new(
                coords.x,
                yCoord,
                coords.z);
            Ray ray = new Ray(origin, Vector3.down);
            bool hit = Raycast(ray, out hitPos, out hitNormal, castDistance, raycastMask, physicsScene, queryTriggerInteraction);
            return hit;
        }
        
        static readonly RaycastHit[] Results = new RaycastHit[64];
        static readonly UniversalProfilerMarker FindClosestMarker = new("Ground: Find Closest");

        static (float height, Vector3 normal) FindClosest(Vector3 coords, int raycastMask, Transform ignoreRoot, bool performExtraChecks, PhysicsScene physicsScene,
            QueryTriggerInteraction queryTriggerInteraction) {
            FindClosestMarker.Begin();
            if (!physicsScene.IsValid()) {
                physicsScene = Physics.defaultPhysicsScene;
            }
            Vector3 origin = new(coords.x, MaxRayHeight, coords.z);
            var size = physicsScene.Raycast(origin, Vector3.down, Results, MaxRayHeight + BelowGroundHeight, raycastMask, queryTriggerInteraction);
            if (size <= 0) return (coords.y, Vector3.up);

            int bestHitIndex = -1;
            float bestResult = float.PositiveInfinity;
            float bestDistanceSq = float.PositiveInfinity;
            for (int index = 0; index < size; index++) {
                var hit = Results[index];
                if (performExtraChecks && CheckIfHitIsContainedByAnotherCollider(index, hit, size)) {
                    continue;
                }
                var distanceSq = (hit.point.y - coords.y).Squared();
                if (bestDistanceSq > distanceSq && (ignoreRoot == null || !hit.collider.transform.IsChildOf(ignoreRoot))) {
                    bestDistanceSq = distanceSq;
                    bestResult = hit.point.y;
                    bestHitIndex = index;
                }
            }

            FindClosestMarker.End();
            var height = float.IsPositiveInfinity(bestResult) ? coords.y : bestResult;
            var normal = bestHitIndex == -1 ? Vector3.up : Results[bestHitIndex].normal;
            return (height, normal);
        }

        static float FindClosestNotBelowTerrain(Vector3 coords, int raycastMask, Transform ignoreRoot, PhysicsScene physicsScene = default) {
            FindClosestMarker.Begin();
            Vector3 origin = new(coords.x, MaxRayHeight, coords.z);
            if (!physicsScene.IsValid()) {
                physicsScene = Physics.defaultPhysicsScene;
            }
            int size = physicsScene.Raycast(origin, Vector3.down, Results, MaxRayHeight + BelowGroundHeight, raycastMask);
            if (size <= 0) return coords.y;

            bool terrainHit = physicsScene.Raycast(origin, Vector3.down, out var terrainHitInfo, MaxRayHeight + BelowGroundHeight, RenderLayers.Mask.Terrain);

            float bestResult = float.PositiveInfinity;
            float bestDistanceSq = float.PositiveInfinity;
            float terrainY = terrainHit ? terrainHitInfo.point.y : float.NegativeInfinity;
            for (int index = 0; index < size; index++) {
                var hit = Results[index];
                var hitPointY = hit.point.y;
                if (hitPointY < terrainY) continue;
                var distanceSq = (hitPointY - coords.y).Squared();
                if (bestDistanceSq > distanceSq && (ignoreRoot == null || !hit.collider.transform.IsChildOf(ignoreRoot))) {
                    bestDistanceSq = distanceSq;
                    bestResult = hit.point.y;
                }
            }

            FindClosestMarker.End();
            return float.IsPositiveInfinity(bestResult) ? coords.y : bestResult;
        }

        static bool CheckIfHitIsContainedByAnotherCollider(int indexToIgnore, RaycastHit hit, int resultsSize) {
            for (int i = 0; i < resultsSize; i++) {
                if (i != indexToIgnore) {
                    RaycastHit toCheckContains = Results[i];
                    if (toCheckContains.collider is not null and not MeshCollider { convex: false } and not TerrainCollider) {
                        if (toCheckContains.collider.ClosestPoint(hit.point) == hit.point) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}