using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.FootSteps;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Pathfinding;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility {
    public readonly ref struct NavMeshBakingPreparation {
        readonly List<INavMeshBakingPreparer.IReversible> _toRevert;
        readonly GameObject[] _tempGameObjects;

        public NavMeshBakingPreparation(Scene scene) {
            _toRevert = new List<INavMeshBakingPreparer.IReversible>();
            var groundBounds = Object.FindAnyObjectByType<GroundBounds>(FindObjectsInactive.Exclude);
            _tempGameObjects = null;
            if (groundBounds != null) {
                _tempGameObjects = GeneratePolygonBoundsNavmeshBlockingColliders(new GroundBounds.EditorAccess(groundBounds));
            }

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots) {
                if (root.activeSelf) {
                    foreach (var withColliders in root.GetComponentsInChildren<INavMeshBakingPreparer>()) {
                        var reversible = withColliders.Prepare();
                        if (reversible != null) {
                            _toRevert.Add(reversible);
                        }
                    }
                }
            }
        }

        static GameObject[] GeneratePolygonBoundsNavmeshBlockingColliders(GroundBounds.EditorAccess groundBounds) {
            var polygon2DAuthoring = groundBounds.GameBoundsPolygon;
            var polygon2d = polygon2DAuthoring.ToPolygon(ARAlloc.Temp);
            if (polygon2d.Length == 0) {
                polygon2d.Dispose();
                return null;
            }
            // Make size in normal direction big enough to cover all remaining area
            var colliderSizeInNormalDirection = math.cmax(polygon2d.bounds.Extents * 2);
            var midPosY = (groundBounds.BoundsTop + groundBounds.BoundsBottom) * 0.5f;
            var sizeY = (groundBounds.BoundsTop - groundBounds.BoundsBottom);
            var convexPolygonPoints = Algorithms2D.GetPolygon2dConvexHull(polygon2d.points.AsNativeArray(), ARAlloc.Temp);
            var convexPolygonPointsArr = convexPolygonPoints.AsArray();
            var pointsCount = convexPolygonPointsArr.Length;
            if (pointsCount == 0) {
                polygon2d.Dispose();
                convexPolygonPoints.Dispose();
                return null;
            }
            var centroid2d = Algorithms2D.GetCentroid(convexPolygonPointsArr);
            var blockingColliders = new GameObject[pointsCount];
            for (int i = 0; i < pointsCount; i++) {
                var startPos = convexPolygonPointsArr[i];
                var endPos = convexPolygonPointsArr[(i + 1) % pointsCount];
                var blockingCollider = CreateNavmeshBlockingCollider(startPos, endPos, centroid2d, colliderSizeInNormalDirection,
                    midPosY, sizeY);
                blockingColliders[i] = blockingCollider;
            }
            polygon2d.Dispose();
            convexPolygonPoints.Dispose();
            return blockingColliders;

            static GameObject CreateNavmeshBlockingCollider(float2 startPos2d, float2 endPos2d, float2 centroid2d,
                float sizeInNormalDirection, float posY, float sizeY) {
                var lineVector2d = endPos2d - startPos2d;
                var colliderGO = new GameObject("navmeshBlocker", typeof(MeshSurfaceType),
                    typeof(BoxCollider));
                var meshSurfaceType = colliderGO.GetComponent<MeshSurfaceType>();
                meshSurfaceType.Mode = RecastMeshObjStatic.Mode.UnwalkableSurface;
                var lineVector = lineVector2d.x0y();
                var startPos = new float3(startPos2d.x, posY, startPos2d.y);
                var boxTransformPos = (startPos + (lineVector * 0.5f));
                var lineDirection = math.normalize(lineVector);
                var boxTransformRot = Quaternion.LookRotation(lineDirection, Vector3.up);
                colliderGO.transform.SetPositionAndRotation(boxTransformPos, boxTransformRot);
                var colliderNormalDir2d = colliderGO.transform.right.xz();
                var colliderPos2d = boxTransformPos.xz;
                var centroidToPosDir2d = colliderPos2d - centroid2d;
                if (math.dot(colliderNormalDir2d, centroidToPosDir2d) > 0) {
                    colliderGO.transform.rotation = Quaternion.LookRotation(-lineDirection, Vector3.up);
                }
                var boxCollider = colliderGO.GetComponent<BoxCollider>();
                var edgeLength = math.length(lineVector);
                // Make size on Z 5x the edge length to ensure that it blocks the navmesh on corners of convex hull  
                var boxSize = new Vector3(sizeInNormalDirection, sizeY, edgeLength * 5);
                boxCollider.size = boxSize;
                boxCollider.center = new Vector3(-boxSize.x * 0.5f, 0, 0);
                return colliderGO;
            }
        }
        
        public void Dispose() {
            foreach (var toRevert in _toRevert) {
                toRevert.Revert();
            }

            _toRevert.Clear();
            if (_tempGameObjects != null) {
                for (int i = 0; i < _tempGameObjects.Length; i++) {
                   Object.DestroyImmediate(_tempGameObjects[i]);
                }
            }
        }

        
    }
}