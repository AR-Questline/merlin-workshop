using Awaken.CommonInterfaces;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.GameObjects;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.FootSteps {
    [DisallowMultipleComponent]
    public class MeshSurfaceType : MonoBehaviour, INavMeshBakingPreparer {
        public const string AllowedSurfaceType = "TerrainType";

        [SerializeField, RichEnumExtends(typeof(SurfaceType), new[] {AllowedSurfaceType}, true)]
        RichEnumReference surfaceType;
#if UNITY_EDITOR
        [SerializeField, Title("Pathfinding")] bool isTunnel;
        public RecastMeshObjStatic.Mode Mode { get; set; } = RecastMeshObjStatic.Mode.WalkableSurface;
#endif

        public SurfaceType SurfaceType => surfaceType.EnumAs<SurfaceType>();
        
        public INavMeshBakingPreparer.IReversible Prepare() {
#if UNITY_EDITOR
            if (isTunnel) {
                return null;
            }
            var collider = GetComponentInChildren<Collider>();
            if (collider == null) {
                return null;
            }
            if (gameObject.GetComponent<RecastMeshObjStatic>() != null) {
                return null;
            }
            var recastObject = gameObject.AddComponent<RevertableRecastMeshObjStatic>();
            recastObject.solid = true;
            recastObject.geometrySource = RecastMeshObjStatic.GeometrySource.Collider;
            recastObject.includeInScan = RecastMeshObjStatic.ScanInclusion.AlwaysInclude;
            recastObject.mode = Mode;
            recastObject.waterProperties = default;
            recastObject.surfaceID = 1;
            recastObject.Init(collider);
            
            return recastObject;
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        public void EDITOR_Init(SurfaceType type) {
            surfaceType = new RichEnumReference(type);
        }
#endif
    }

#if UNITY_EDITOR
    public class RevertableRecastMeshObjStatic : RecastMeshObjStatic, INavMeshBakingPreparer.IReversible {
        Collider _collider;
        Bounds _bounds;
        
        public void Init(Collider collider) {
            _collider = collider;
            _bounds = _collider.bounds;
        }
        
        public void Revert() {
            GameObjects.DestroySafely(this);
        }

        protected override Bounds CalculateBounds() => _bounds;

        public override void ResolveMeshSource(out MeshFilter meshFilter, out Collider collider) {
            meshFilter = null;
            collider = _collider;
        }
    }
#endif
}