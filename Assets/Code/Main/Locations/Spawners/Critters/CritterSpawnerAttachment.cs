using System;
using System.Collections.Generic;
using Awaken.ECS.Critters;
using Awaken.ECS.Critters.Components;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using FMODUnity;
using Pathfinding;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners.Critters {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Spawns critters in area.")]
    public class CritterSpawnerAttachment : MonoBehaviour, IAttachmentSpec {
        const float DirectionDotAlignThreshold = 0.99f;
        const float DirectionDotCheckVectorMinLength = 0.01f;

        [field: SerializeField] public GameObject CritterVisualsPrefab { get; private set; }
        [field: SerializeField] public GameObject CritterLogicPrefab { get; private set; }
        [field: SerializeField, Range(1, 50)] public float SpawnRadius { get; private set; } = 10f;
        [field: SerializeField, Range(1, 100)] public float MovementCullingDistance { get; private set; } = 30f;
        [field: SerializeField, Range(1, CritterPools.PoolCapacity)] public int Count { get; private set; } = 15;
        [field: SerializeField] public bool UsePaths { get; private set; }
        [field: SerializeField, ShowIf(nameof(UsePaths)), Range(2, 20)] public int PointsPerPath { get; private set; } = 10;
        [field: SerializeField, ShowIf(nameof(UsePaths)), Min(0.001f)] public float SubSamplesDensity { get; private set; } = 0.01f;
        [field: SerializeField, ShowIf(nameof(UsePaths)), Min(0.001f)] public float PointsMinDistance { get; private set; } = 0.05f;
        [field: SerializeField] public float CritterMinScale { get; private set; } = 1f;
        [field: SerializeField] public float CritterMaxScale { get; private set; } = 1.2f;

        [field: SerializeField] public CritterMovementParams MovementParams { get; private set; } = new(90f, 1f, 2f, 2f, 5f, 0.5f);
        [SerializeField] public bool limitPathsCheckHeight;
        [SerializeField, ShowIf(nameof(limitPathsCheckHeight))] public float pathsMaxCheckHeight = 3.5f;
        [field: SerializeField, BoxGroup("Audio")] public EventReference IdleSound { get; private set; }
        [field: SerializeField, BoxGroup("Audio")] public EventReference MovementSound { get; private set; }
        [field: SerializeField, BoxGroup("Audio")] public EventReference DeathSound { get; private set; }
        [field: SerializeField, TemplateType(typeof(LocationTemplate))] public TemplateReference DropTemplateRef { get; private set; }

        [ShowInInspector] public int PathsCount => _paths.Count;
        public CritterSoundsGuids Sounds => new(IdleSound.Guid, MovementSound.Guid);
        public PathsArray Paths => _paths;

        [HideInInspector, SerializeField] PathsArray _paths;
        CritterSpawner _spawner;

        public Element SpawnElement() {
            _spawner = new CritterSpawner();
            return _spawner;
        }

        public bool IsMine(Element element) => element is CritterSpawner;

        [Button]
        public void GeneratePaths() {
            if (AstarPath.active == null || AstarPath.active.data == null || AstarPath.active.data.graphs == null) {
                Log.Minor?.Error("No AStarPath");
                return;
            }

            bool isScanned = IsAStarPathScanned();
            if (!isScanned) {
                if (AstarPath.active.data.cacheStartup && AstarPath.active.data.hasCacheFile) {
                    AstarPath.active.data.LoadFromCache();
                } else {
                    AstarPath.active.Scan();
                }
            }

            var spec = GetComponent<LocationSpec>();
            var seeker = spec.GetOrAddComponent<Seeker>();
            seeker.startEndModifier.exactStartPoint = StartEndModifier.Exactness.ClosestOnNode;
            seeker.startEndModifier.exactEndPoint = StartEndModifier.Exactness.ClosestOnNode;
            var funnelModifier = spec.GetOrAddComponent<FunnelModifier>();
            funnelModifier.quality = FunnelModifier.FunnelQuality.Medium;
            var pathsPoints = CritterPathsGenerator.GeneratePathsInEditor(Count, PointsPerPath, transform.position, SpawnRadius, seeker);

            GameObjects.DestroySafely(seeker);
            GameObjects.DestroySafely(funnelModifier);

            _paths = GeneratePaths(pathsPoints);
        }

        [Button]
        public void ClearPaths() {
            _paths = default;
        }

        PathsArray GeneratePaths(Vector3[][] pathsPoints) {
            List<CritterPathPointData> newPathPoints = new(Count * PointsPerPath);
            PathsArray paths = new();
            List<PathsArray.StartIndexAndLength> pathsStartIndicesAndLength = new(Count);
            var invSubSamplesDensity = 1 / SubSamplesDensity;
            int startIndex = 0;
            for (int i = 0; i < Count; i++) {
                var path = pathsPoints[i];
                var prevPointsCount = newPathPoints.Count;
                var rayStartY = limitPathsCheckHeight ? transform.position.y + pathsMaxCheckHeight : Ground.MaxRayHeight;
                SubSamplePath(path, invSubSamplesDensity, math.square(PointsMinDistance), rayStartY, newPathPoints);
                var pathPointsCount = newPathPoints.Count - prevPointsCount;
                if (pathPointsCount == 0) {
                    continue;
                }

                pathsStartIndicesAndLength.Add(new PathsArray.StartIndexAndLength(startIndex, pathPointsCount));
                startIndex += pathPointsCount;
            }

            paths.Paths = newPathPoints.ToArray();
            paths.PathsRanges = pathsStartIndicesAndLength.ToArray();
            return paths;
        }

        static void SubSamplePath(ArraySegment<Vector3> path, float invSubSamplesDensity, float pointsMinDistanceSq, float rayStartY,
            List<CritterPathPointData> outputPathPoints) {
            for (int i = 0; i < path.Count; i++) {
                var pathLineStart = path[i];
                SnapToGround(ref pathLineStart, rayStartY);
                // Normal is set later, in next loop
                outputPathPoints.Add(new(pathLineStart, float3.zero));
                var pathLineEnd = path[(i + 1) % path.Count];
                SnapToGround(ref pathLineEnd, rayStartY);
                SubSamplePathLine(pathLineStart, pathLineEnd, invSubSamplesDensity, pointsMinDistanceSq, rayStartY, outputPathPoints);
            }

            int pathPointsCount = outputPathPoints.Count;
            for (int i = 0; i < pathPointsCount; i++) {
                var currentPoint = outputPathPoints[i];
                var nextPoint = outputPathPoints[(i + 1) % pathPointsCount];
                var segmentNormal = GetNormalAtPoint(Vector3.Lerp(currentPoint.position, nextPoint.position, 0.5f));
                outputPathPoints[i] = new CritterPathPointData(outputPathPoints[i].position, segmentNormal);
            }
        }

        static void SubSamplePathLine(Vector3 pathLineStart, Vector3 pathLineEnd, float invSubSampleDensity, float pointsMinDistanceSq, float rayStartY,
            List<CritterPathPointData> outputPathPoints) {
            var pathLineVector = pathLineEnd - pathLineStart;
            var pathLineLength = pathLineVector.magnitude;
            var subSamplesCount = Mathf.CeilToInt(pathLineLength * invSubSampleDensity) - 1;
            if (Mathf.Approximately(pathLineLength, 0) || subSamplesCount == 0) {
                return;
            }

            var pathDir = pathLineVector / pathLineLength;
            float subSampleDistance = pathLineLength / (subSamplesCount + 1);
            List<Vector3> subSamplePoints = new(subSamplesCount);
            for (int i = 1; i < subSamplesCount + 1; i++) {
                var subSamplePos = pathLineStart + (pathDir * (i * subSampleDistance));
                SnapToGround(ref subSamplePos, rayStartY);
                subSamplePoints.Add(subSamplePos);
            }

            //Algorithm which adds sub-sampled points excluding all redundant
            //points laying on the same straight line 
            Vector3 currentDir;
            var prevPoint = subSamplePoints[0];
            var lastCompletedDirection = (prevPoint - pathLineStart).normalized;
            var lastAddedPathPoint = pathLineStart;
            for (int i = 1; i < subSamplePoints.Count; i++) {
                Vector3 currentPoint = subSamplePoints[i];
                var vectorBetweenCurrentAndPrevPoint = (currentPoint - prevPoint);
                var distanceBetweenCurrentAndPrevPoint = vectorBetweenCurrentAndPrevPoint.magnitude;

                currentDir = vectorBetweenCurrentAndPrevPoint / distanceBetweenCurrentAndPrevPoint;
                if (distanceBetweenCurrentAndPrevPoint > DirectionDotCheckVectorMinLength &&
                    (lastAddedPathPoint - prevPoint).sqrMagnitude > pointsMinDistanceSq && Vector3.Dot(currentDir, lastCompletedDirection) < DirectionDotAlignThreshold) {
                    outputPathPoints.Add(new(prevPoint, float3.zero));
                    lastAddedPathPoint = prevPoint;
                    lastCompletedDirection = currentDir;
                }

                prevPoint = currentPoint;
            }

            {
                var vectorBetweenCurrentAndPrevPoint = (pathLineEnd - prevPoint);
                var distanceBetweenCurrentAndPrevPoint = vectorBetweenCurrentAndPrevPoint.magnitude;

                currentDir = vectorBetweenCurrentAndPrevPoint / distanceBetweenCurrentAndPrevPoint;
                if (distanceBetweenCurrentAndPrevPoint > DirectionDotCheckVectorMinLength &&
                    (lastAddedPathPoint - prevPoint).sqrMagnitude > pointsMinDistanceSq && Vector3.Dot(currentDir, lastCompletedDirection) < DirectionDotAlignThreshold) {
                    outputPathPoints.Add(new(prevPoint, float3.zero));
                }
            }
        }

        static Vector3 GetNormalAtPoint(Vector3 point) {
            Ground.TryHit(point, Ground.LayerMask, Ground.MaxRayHeight, Ground.MaxRayHeight + Ground.BelowGroundHeight, out Vector3 _, out Vector3 hitNormal, default,
                QueryTriggerInteraction.Ignore);
            return hitNormal;
        }

        static void SnapToGround(ref Vector3 point, float rayStartY) {
            var raycastMask = RenderLayers.Mask.Default | RenderLayers.Mask.Walkable | RenderLayers.Mask.Objects |
                              RenderLayers.Mask.Terrain | RenderLayers.Mask.Hitboxes | RenderLayers.Mask.HLOD;
            var snappedPointHeight = Ground.HeightAt(point, raycastMask, findClosest: false, rayStartY: rayStartY, 
                queryTriggerInteraction: QueryTriggerInteraction.Ignore);
            if (snappedPointHeight == 0) {
                snappedPointHeight = point.y;
            }

            point.y = snappedPointHeight;
        }

        static bool IsAStarPathScanned() {
            var graphs = AstarPath.active.data.graphs;
            var graphsCount = graphs.Length;
            for (int i = 0; i < graphsCount; i++) {
                var graph = graphs[i];
                if (graph != null && !graph.isScanned) {
                    return false;
                }
            }

            return true;
        }

#if UNITY_EDITOR
        void OnValidate() {
            if (_spawner != null) {
                _spawner.EDITOR_UpdateEntitiesData(this);
            }
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, SpawnRadius);

            if (limitPathsCheckHeight) {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position + new Vector3(0, pathsMaxCheckHeight, 0), new Vector3(SpawnRadius, 0.001f, SpawnRadius));
            }
            var random = new Unity.Mathematics.Random(525);
            if (_paths.IsValid) {
                for (int pathIndex = 0; pathIndex < _paths.Count; pathIndex++) {
                    var dest = _paths.GetPathPoints(pathIndex);
                    if (dest.Count == 0) {
                        return;
                    }

                    var randomColorValue = random.NextFloat3();
                    var pathColor = new Color(randomColorValue.x, randomColorValue.y, randomColorValue.z, 1);
                    for (int i = 0; i < dest.Count; i++) {
                        Gizmos.color = pathColor;
                        var point = dest[i];
                        var nextPoint = dest[(i + 1) % dest.Count];
                        Gizmos.DrawLine(point.position, nextPoint.position);
                        Gizmos.DrawSphere(point.position, 0.02f);
                        var midPoint = Vector3.Lerp(point.position, nextPoint.position, 0.5f);
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(midPoint, point.Normal);
                    }

                    Gizmos.color = Color.white;
                }
            }
            
        }
#endif
    }
}