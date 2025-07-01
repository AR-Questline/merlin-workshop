using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.ProceduralMeshes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using QFSW.QC;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Graphics.DayNightSystem {
    [ExecuteInEditMode]
    public class WyrdnightSplineRepeller : MonoBehaviour, IWyrdnightRepellerSource {
        static readonly int PositionID = Shader.PropertyToID("position");
        static readonly int EventNameID = Shader.PropertyToID("direct");
        
        [SerializeField] VisualEffect visualEffect;
        [SerializeField] uint vfxPointCount = 10;
        [SerializeField] float maxDensity = 5;
        [SerializeField] float raycastHeight = 200f;
        [SerializeField] float distanceMultiplier = 1.2f;
        
        [Title("Baked data")]
        [MeshAssetReference] public ShareableARAssetReference repellerMesh;
        [SerializeField, ReadOnly] SerializedPolygon2D repellerPolygon;
        [NonSerialized] Polygon2D _runtimePolygon = Polygon2D.Invalid;

        public SerializedPolygon2D RepellerPolygon => repellerPolygon;

        QueryParameters _queryParameters;
        Matrix4x4 _worldToLocal;
        float _distanceToHeroSq;
        VFXEventAttribute _eventAttribute;
        
        void Awake() {
            if (!Application.isPlaying) return;
            
            Transform transformCache = transform;
            
            if (_runtimePolygon.points.IsCreated) {
                Log.Critical?.Error("Runtime polygon points are already created. This should not happen.", this);
            } else {
                _runtimePolygon = repellerPolygon.ToPolygon(transformCache);
            }
            
            World.Services.Get<WyrdnessService>().RegisterRepeller(this);

            if (visualEffect == null) {
                return;
            }

            _eventAttribute = visualEffect.CreateVFXEventAttribute(); 
            _distanceToHeroSq = math.square(LocationCullingGroup.LocationDistanceBands[LocationCullingGroup.LastBand - 1] * distanceMultiplier);
            _worldToLocal = transformCache.worldToLocalMatrix;
            _queryParameters = new QueryParameters {
                layerMask = Ground.LayerMask,
                hitTriggers = QueryTriggerInteraction.Ignore,
                hitBackfaces = false,
                hitMultipleFaces = false
            };
        }

        void OnEnable() {
            if (!Application.isPlaying) return;
            if (visualEffect == null) return;
            
            World.Services.Get<UnityUpdateProvider>().RegisterSplineRepeller(this);
        }

        void OnDisable() {
            if (!Application.isPlaying) return;
            World.Services.TryGet<UnityUpdateProvider>()?.UnregisterSplineRepeller(this);
        }

        void OnDestroy() {
            _runtimePolygon.CheckedDispose();
            _runtimePolygon = Polygon2D.Invalid;
            _eventAttribute?.Dispose();

            if (!Application.isPlaying) return;
            World.Services.TryGet<WyrdnessService>()?.UnregisterRepeller(this);
        }

        public void UnityUpdate(float deltaTime) {
            // send events to visual effect at positions generated at random from runtime polygon
            if (visualEffect == null || Hero.Current == null || World.Services.Get<SceneService>().IsAdditiveScene || deltaTime == 0) return;
            
            var random = new Unity.Mathematics.Random((uint) Time.frameCount);
            Polygon2DUtils.SegmentsInRadius(_runtimePolygon.points, Hero.Current.Coords.xz(), _distanceToHeroSq, ARAlloc.Temp, out var nearbySegments);
            Algorithms2D.RandomPointsOnSegments(nearbySegments, vfxPointCount, maxDensity, ref random, ARAlloc.Temp, out var points);
            Ground.Snap2DPointsToGround(points, raycastHeight, _queryParameters, ARAlloc.Temp, out var resultPoints);
            
            for (var i = 0u; i < resultPoints.Length; i++) {
                var position = resultPoints[i];
                _eventAttribute.SetVector3(PositionID, _worldToLocal.MultiplyPoint3x4(position));
                visualEffect.SendEvent(EventNameID, _eventAttribute);
            }
            nearbySegments.Dispose();
            points.Dispose();
            resultPoints.Dispose();
        }
        
        public bool IsPositionInRepeller(Vector3 position) {
            var float2 = (float2)position.XZ();
            if (!_runtimePolygon.bounds.Contains(float2)) {
                return false;
            }
            
            Polygon2DUtils.IsInPolygon(float2, _runtimePolygon, out var isIn);
            return isIn;
        }

#if UNITY_EDITOR
        [Title("Editor References"), AssetsOnly]
        public Mesh generatedMesh;
        [SerializeField] SplineMeshGenerator splineMeshGenerator;
        [SerializeField] SplineContainer splineContainer;
        [SerializeField] MeshFilter meshFilter;
        
        [ShowInInspector, PropertyOrder(-1)]
        bool SplineIsClosed => splineContainer != null && splineContainer.Spline.Closed;
        [NonSerialized] bool _initialized;

        void Start() {
            if (Application.isPlaying) return;
            Reset();
            OnValidate();
        }

        void Reset() {
            splineMeshGenerator = GetComponentInChildren<SplineMeshGenerator>();
            
            if (splineMeshGenerator == null) {
                var go = new GameObject("SplineMeshGenerator");
                go.transform.SetParent(transform);
                splineMeshGenerator = go.AddComponent<SplineMeshGenerator>();
            }
            
            splineContainer = GetComponentInChildren<SplineContainer>();
            meshFilter = GetComponentInChildren<MeshFilter>();
        }
        
        void OnValidate() {
            if (Application.isPlaying) return;
            if (!_initialized && splineMeshGenerator != null) {
                if (splineMeshGenerator.generationMode != SplineMeshGenerator.MeshGenerationMode.Vertical) {
                    splineMeshGenerator.generationMode = SplineMeshGenerator.MeshGenerationMode.Vertical;
                    EditorUtility.SetDirty(splineMeshGenerator);
                }
                
                if (repellerPolygon.IsEmpty) {
                    repellerPolygon = new SerializedPolygon2D(splineMeshGenerator.GetPointsOnSpline());
                    EditorUtility.SetDirty(this);
                }
                
                splineMeshGenerator.OnMeshGenerated += () => {
                    generatedMesh = splineMeshGenerator.mesh;
                    repellerPolygon = new SerializedPolygon2D(splineMeshGenerator.GetPointsOnSpline());
                    EditorUtility.SetDirty(this);
                };
                _initialized = true;
                return;
            }
            
            if (meshFilter != null) {
                if (meshFilter.sharedMesh != null && meshFilter.sharedMesh != generatedMesh) {
                    generatedMesh = meshFilter.sharedMesh;
                    EditorUtility.SetDirty(this);
                }
            }
        }

        void OnDrawGizmosSelected() {
            if (repellerPolygon.IsEmpty) return;
            var _transform = transform;
            float height = _transform.position.y;
            var points = repellerPolygon.PolygonPoints(_transform);
            for (var i = 0u; i < points.Length; i++) {
                // change gizmo color shade by progress in the spline
                Gizmos.color = Color.Lerp(Color.red, Color.white, i / (float)points.Length);
                Gizmos.DrawSphere(points[i].xcy(height + i * 0.1f), 1);
            }
        }
#endif
    }
}