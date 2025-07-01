using Awaken.TG.Graphics.ProceduralMeshes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using AwesomeTechnologies.VegetationSystem;
using Pathfinding;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;

#if UNITY_EDITOR
using System;
using Awaken.Utility.Debugging;
using UnityEditor;
#endif

namespace Awaken.TG.Main.Grounds {
    /// <summary>
    /// Manages Bounds of terrain
    /// </summary>
    public class GroundBounds : MonoBehaviour, IDomainBoundService {
        public Domain Domain => Domain.CurrentMainScene();
        public bool RemoveOnDomainChange() => true;
        [SerializeField] Color gameColor = new Color(0, 1, 1, 1);
        [SerializeField] Polygon2DAuthoring gameBoundsPolygon;
        [SerializeField] float boundsTop;
        [SerializeField] float boundsBottom;
        [SerializeField] Color pathfindingColor = new Color(0, 0.8f, 1, 1);
        [SerializeField] float seaLevel;
        [SerializeField] bool overrideVspSeaLevel;
        [SerializeField] float vspSeaLevel;
        [SerializeField] float pathfindingInSeaHeight;
        [SerializeField] Color vegetationColor = new Color(0.2f, 1, 0.2f, 1);
        [SerializeField] Polygon2DAuthoring vegetationBoundsPolygon;
        [SerializeField] Color terrainForegroundColor = new Color(1, 0.2f, 0.2f, 1);
        [SerializeField] Polygon2DAuthoring terrainForegroundBoundsPolygon;
        [SerializeField] Color terrainBackgroundColor = new Color(0.7f, 0.2f, 0.2f, 1);
        [SerializeField] Polygon2DAuthoring terrainBackgroundBoundsPolygon;
        [SerializeField] Color medusaColor = new Color(0.2f, 0.2f, 0.2f, 1);
        [SerializeField] Polygon2DAuthoring medusaBoundsPolygon;
        public Bounds CalculateGameBounds() {
            
            var gamePolygon = gameBoundsPolygon.ToPolygon(ARAlloc.Temp);
            var gameBounds = gamePolygon.bounds.ToMinMaxAABB(boundsBottom, boundsTop);
            gamePolygon.Dispose();
            return gameBounds.ToBounds();
        }
        
        public MinMaxAABR CalculateGameBounds2D() {
            
            var gamePolygon = gameBoundsPolygon.ToPolygon(ARAlloc.Temp);
            var gameBounds = gamePolygon.bounds;
            gamePolygon.Dispose();
            return gameBounds;
        }

        public Bounds CalculateVegetationBounds() {
            var vegetationPolygon = vegetationBoundsPolygon.ToPolygon(ARAlloc.Temp);
            var vegetationBounds = vegetationPolygon.bounds.ToMinMaxAABB(boundsBottom, boundsTop);
            vegetationPolygon.Dispose();
            return vegetationBounds.ToBounds();
        }

        public void CalculateGamePolygon(Allocator allocator, out Polygon2D polygon) {
            polygon = gameBoundsPolygon.ToPolygon(allocator);
        }

        public void CalculateTerrainForegroundPolygon(Allocator allocator, out Polygon2D polygon) {
            polygon = terrainForegroundBoundsPolygon.ToPolygon(allocator);
        }

        public void CalculateTerrainBackgroundPolygon(Allocator allocator, out Polygon2D polygon) {
            polygon = terrainBackgroundBoundsPolygon.ToPolygon(allocator);
        }

        public void CalculateMedusaPolygon(Allocator allocator, out Polygon2D polygon) {
            polygon = medusaBoundsPolygon.ToPolygon(allocator);
        }

#if UNITY_EDITOR
        float PathfindingCenterY => (boundsTop + seaLevel - pathfindingInSeaHeight) * 0.5f;
        float PathfindingHeight => boundsTop - (seaLevel - pathfindingInSeaHeight);

        void SetupRelatedSystems() {
            SetupPathfinding();
            try {
                SetupVsp();
            } catch (Exception e) {
                Log.Important?.Error("Failed to setup VSP: " + e);
            }
        }

        void SetupPathfinding() {
            var pathfinder = AstarPath.active ??= FindAnyObjectByType<AstarPath>();
            if (pathfinder == null) {
                Log.Important?.Warning("No Astar graph found on scene: " + SceneManager.GetActiveScene().name);
                return;
            }

            if (pathfinder.data.graphs == null) {
                pathfinder.data.DeserializeGraphs();
            }

            if (pathfinder.graphs.Length <= 0 || pathfinder.graphs[0] is not RecastGraph recastGraph) {
                Log.Important?.Warning($"Astar graph on scene {SceneManager.GetActiveScene().name} is not RecastGraph");
                return;
            }

            var gamePolygon = gameBoundsPolygon.ToPolygon(ARAlloc.Temp);
            var pathfindingBounds = gamePolygon.bounds;
            gamePolygon.Dispose();

            var y = PathfindingCenterY;
            var height = PathfindingHeight;

            var center = pathfindingBounds.Center.xcy(y);
            recastGraph.forcedBoundsCenter = center;

            recastGraph.forcedBoundsSize = pathfindingBounds.Extents.xcy(height);
            EditorUtility.SetDirty(pathfinder);
        }

        void SetupVsp() {
            // var vsp = FindAnyObjectByType<VegetationSystemPro>();
            // if (vsp == null) {
            //     return;
            // }
            //
            // var vegetationPolygon = vegetationBoundsPolygon.ToPolygon(ARAlloc.Temp);
            // var vegetationBounds = vegetationPolygon.bounds.ToMinMaxAABB(boundsBottom, boundsTop).ToBounds();
            // vegetationPolygon.Dispose();
            //
            // if (overrideVspSeaLevel) {
            //     vsp.SeaLevel = vspSeaLevel;
            // } else {
            //     vsp.SeaLevel = seaLevel - boundsBottom;
            // }
            // vsp.VegetationSystemBounds = vegetationBounds;
            //
            // var terrain = FindAnyObjectByType<RaycastTerrain>() ?? gameObject.AddComponent<RaycastTerrain>();
            // terrain.RaycastLayerMask = RenderLayers.Mask.Terrain;
            // var worldToLocal = terrain.transform.worldToLocalMatrix;
            // terrain.RaycastTerrainBounds = vegetationBounds.Transform(worldToLocal);
            // EditorUtility.SetDirty(terrain);
            // vsp.RemoveAllTerrains();
            // vsp.AddTerrain(terrain.gameObject);
            //
            // EditorUtility.SetDirty(vsp);
        }

        public readonly struct EditorAccess {
            public readonly GroundBounds groundBounds;

            public Color GameColor => groundBounds.gameColor;
            public ref Polygon2DAuthoring GameBoundsPolygon => ref groundBounds.gameBoundsPolygon;
            public ref float BoundsTop => ref groundBounds.boundsTop;
            public ref float BoundsBottom => ref groundBounds.boundsBottom;
            public Color PathfindingColor => groundBounds.pathfindingColor;
            public ref float SeaLevel => ref groundBounds.seaLevel;
            public ref bool OverrideVspSeaLevel => ref groundBounds.overrideVspSeaLevel;
            public ref float VspSeaLevel => ref groundBounds.vspSeaLevel;
            public float PathfindingInSeaHeight => groundBounds.pathfindingInSeaHeight;
            public float PathfindingCenterY => groundBounds.PathfindingCenterY;
            public float PathfindingHeight => groundBounds.PathfindingHeight;
            public Color VegetationColor => groundBounds.vegetationColor;
            public ref Polygon2DAuthoring VegetationBoundsPolygon => ref groundBounds.vegetationBoundsPolygon;
            public Color TerrainForegroundColor => groundBounds.terrainForegroundColor;
            public ref Polygon2DAuthoring TerrainForegroundBoundsPolygon => ref groundBounds.terrainForegroundBoundsPolygon;
            public Color TerrainBackgroundColor => groundBounds.terrainBackgroundColor;
            public ref Polygon2DAuthoring TerrainBackgroundBoundsPolygon => ref groundBounds.terrainBackgroundBoundsPolygon;
            public Color MedusaColor => groundBounds.medusaColor;
            public ref Polygon2DAuthoring MedusaBoundsPolygon => ref groundBounds.medusaBoundsPolygon;

            public EditorAccess(GroundBounds groundBounds) {
                this.groundBounds = groundBounds;
            }

            public void SetupRelatedSystems() {
                groundBounds.SetupRelatedSystems();
            }
        }
#endif
    }
}
