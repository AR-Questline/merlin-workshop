using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Main.Grounds;
using Awaken.Utility.Editor.Maths;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Terrains {
    [CustomEditor(typeof(GroundBounds))]
    public class GroundBoundsEditor : UnityEditor.Editor {
        static readonly GUIContent PathfindingInSeaHeightContent = new GUIContent("In sea height:");
        static readonly GUIContent TopContent = new GUIContent("Top");
        static readonly GUIContent BottomContent = new GUIContent("Bottom");

        bool _showGame = true;
        bool _showPathfinding;
        bool _showVegetation = true;
        bool _showTerrainForeground = true;
        bool _showTerrainBackground = true;
        bool _showMedusa = true;
        
        bool _editGameBounds;
        bool _editVegetationBounds;
        bool _editTerrainForegroundBounds;
        bool _editTerrainBackgroundBounds;
        bool _editMedusaBounds;

        GroundBounds GroundBounds => (GroundBounds)target;

        public override void OnInspectorGUI() {
            serializedObject.Update();
            var access = new GroundBounds.EditorAccess(GroundBounds);

            GUILayout.BeginVertical("box");
            DrawPolygonSetup("Game:", "gameColor", "gameBoundsPolygon", ref _editGameBounds, ref _showGame);
            if (_showGame) {
                _showPathfinding = false;
            }

            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Bounds:");
            EditorGUIUtility.labelWidth = 35;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("boundsTop"), TopContent);
            EditorGUIUtility.labelWidth = 50;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("boundsBottom"), BottomContent);
            EditorGUIUtility.labelWidth = 60;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("seaLevel"));
            EditorGUILayout.EndHorizontal();
            
            EditorGUIUtility.labelWidth = oldLabelWidth;

            EditorGUILayout.BeginHorizontal();
            _showPathfinding = EditorGUILayout.ToggleLeft("Pathfinding", _showPathfinding, GUILayout.Width(100));
            if (_showPathfinding) {
                _showGame = false;
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pathfindingColor"), GUIContent.none, GUILayout.Width(120));
            EditorGUIUtility.labelWidth = 85;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pathfindingInSeaHeight"), PathfindingInSeaHeightContent);
            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUILayout.EndHorizontal();
            DrawPolygonSetup("Vegetation:", "vegetationColor", "vegetationBoundsPolygon", ref _editVegetationBounds,
                ref _showVegetation);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Override VSP Sea Level", GUILayout.Width(150));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideVspSeaLevel"), GUIContent.none);
            if (access.OverrideVspSeaLevel) {
                EditorGUILayout.LabelField("VSP Sea", GUILayout.Width(50));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("vspSeaLevel"), GUIContent.none, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawPolygonSetup("Foreground:", "terrainForegroundColor", "terrainForegroundBoundsPolygon",
                ref _editTerrainForegroundBounds, ref _showTerrainForeground);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawPolygonSetup("Background:", "terrainBackgroundColor", "terrainBackgroundBoundsPolygon",
                ref _editTerrainBackgroundBounds, ref _showTerrainBackground);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawPolygonSetup("Medusa:", "medusaColor", "medusaBoundsPolygon", ref _editMedusaBounds, ref _showMedusa);
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Refresh")) {
                access.SetupRelatedSystems();
            }

            var allEmpty = !access.GameBoundsPolygon &&
                           !access.VegetationBoundsPolygon &&
                           !access.TerrainForegroundBoundsPolygon &&
                           !access.TerrainBackgroundBoundsPolygon &&
                           !access.MedusaBoundsPolygon;

            if (allEmpty && GUILayout.Button("Crete single common")) {
                CreateSingleCommon(access);
            }
            if (allEmpty && GUILayout.Button("Crete separate")) {
                CreateSeparateCommon(access);
            }
        }

        void DrawPolygonSetup(string name, string colorName, string polygonName, ref bool edit, ref bool show) {
            var colorProperty = serializedObject.FindProperty(colorName);
            var polygonProperty = serializedObject.FindProperty(polygonName);
            EditorGUILayout.BeginHorizontal();
            show = EditorGUILayout.ToggleLeft(name, show, GUILayout.Width(100));

            EditorGUI.BeginChangeCheck();
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 40;
            edit = EditorGUILayout.Toggle("Edit:", edit, GUILayout.Width(60));
            EditorGUIUtility.labelWidth = oldLabelWidth;
            if (EditorGUI.EndChangeCheck() && edit) {
                _editGameBounds = false;
                _editVegetationBounds = false;
                _editTerrainForegroundBounds = false;
                _editTerrainBackgroundBounds = false;
                _editMedusaBounds = false;
                edit = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colorProperty, GUIContent.none, GUILayout.Width(60));
            EditorGUILayout.PropertyField(polygonProperty, GUIContent.none, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck()) {
                SetupColor(colorProperty.colorValue, (Polygon2DAuthoring)polygonProperty.objectReferenceValue);
            }
            EditorGUILayout.EndHorizontal();
        }

        void OnSceneGUI() {
            var access = new GroundBounds.EditorAccess(GroundBounds);
            if (access.GameBoundsPolygon) {
                var polygonEditor = GetEditor(access.GameBoundsPolygon);
                if (_showGame) {
                    polygonEditor.OnSceneGUI(access.BoundsBottom, access.BoundsTop, _editGameBounds);
                } else if (_showPathfinding) {
                    var pathfindingHeight = access.PathfindingHeight;
                    var pathfindingCenterY = access.PathfindingCenterY;
                    var bottom = pathfindingCenterY - pathfindingHeight * 0.5f;
                    var top = pathfindingCenterY + pathfindingHeight * 0.5f;
                    polygonEditor.OnSceneGUI(bottom, top, colorOverride: access.PathfindingColor);
                }
            }
            if (_showVegetation && access.VegetationBoundsPolygon) {
                var polygonEditor = GetEditor(access.VegetationBoundsPolygon);
                var color = Color.Lerp(access.VegetationColor, Color.blue, 0.65f);
                polygonEditor.OnSceneGUI(access.SeaLevel, false, color);
                polygonEditor.OnSceneGUI(access.BoundsBottom, access.BoundsTop, _editVegetationBounds);
            }
            if (_showTerrainForeground && access.TerrainForegroundBoundsPolygon) {
                GetEditor(access.TerrainForegroundBoundsPolygon).OnSceneGUI(0, _editTerrainForegroundBounds);
            }
            if (_showTerrainBackground && access.TerrainBackgroundBoundsPolygon) {
                GetEditor(access.TerrainBackgroundBoundsPolygon).OnSceneGUI(0, _editTerrainBackgroundBounds);
            }
            if (_showMedusa && access.MedusaBoundsPolygon) {
                GetEditor(access.MedusaBoundsPolygon).OnSceneGUI(0, _editMedusaBounds);
            }
        }

        Polygon2DAuthoringEditor GetEditor(Polygon2DAuthoring polygon) {
            return (Polygon2DAuthoringEditor)UnityEditor.Editor.CreateEditor(polygon);
        }

        void SetupColor(in Color color, Polygon2DAuthoring polygon) {
            if (polygon) {
                new Polygon2DAuthoring.EditorAccess(polygon).GizmosColor = color;
            }
        }

        void CreateSingleCommon(GroundBounds.EditorAccess access) {
            var common = new GameObject("Common", typeof(Polygon2DAuthoring));
            common.transform.SetParent(access.groundBounds.transform);

            var minMaxAABB = CollectSceneAABB();

            var commonPolygon = CreatePolygon(common, minMaxAABB);

            access.GameBoundsPolygon = commonPolygon;
            access.VegetationBoundsPolygon = commonPolygon;
            access.TerrainForegroundBoundsPolygon = commonPolygon;
            access.TerrainBackgroundBoundsPolygon = commonPolygon;
            access.MedusaBoundsPolygon = commonPolygon;

            access.BoundsTop = minMaxAABB.Max.y;
            access.BoundsBottom = minMaxAABB.Min.y;
            access.SeaLevel = minMaxAABB.Min.y;

            access.SetupRelatedSystems();
            EditorUtility.SetDirty(target);
        }

        void CreateSeparateCommon(GroundBounds.EditorAccess access) {
            var minMaxAABB = CollectSceneAABB();

            var gameBounds = new GameObject("Gameplay", typeof(Polygon2DAuthoring));
            gameBounds.transform.SetParent(access.groundBounds.transform);
            access.GameBoundsPolygon = CreatePolygon(gameBounds, minMaxAABB);

            var vegetationBounds = new GameObject("Vegetation", typeof(Polygon2DAuthoring));
            vegetationBounds.transform.SetParent(access.groundBounds.transform);
            access.VegetationBoundsPolygon = CreatePolygon(vegetationBounds, minMaxAABB);

            var terrainForegroundBounds = new GameObject("TerrainForeground", typeof(Polygon2DAuthoring));
            terrainForegroundBounds.transform.SetParent(access.groundBounds.transform);
            access.TerrainForegroundBoundsPolygon = CreatePolygon(terrainForegroundBounds, minMaxAABB);

            var terrainBackgroundBounds = new GameObject("TerrainBackground", typeof(Polygon2DAuthoring));
            terrainBackgroundBounds.transform.SetParent(access.groundBounds.transform);
            access.TerrainBackgroundBoundsPolygon = CreatePolygon(terrainBackgroundBounds, minMaxAABB);

            var medusaBounds = new GameObject("Medusa", typeof(Polygon2DAuthoring));
            medusaBounds.transform.SetParent(access.groundBounds.transform);
            access.MedusaBoundsPolygon = CreatePolygon(medusaBounds, minMaxAABB);

            access.BoundsTop = minMaxAABB.Max.y;
            access.BoundsBottom = minMaxAABB.Min.y;
            access.SeaLevel = minMaxAABB.Min.y;

            access.SetupRelatedSystems();
            EditorUtility.SetDirty(target);
        }

        static MinMaxAABB CollectSceneAABB() {
            var minMaxAABB = MinMaxAABB.Empty;
            var meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var meshRenderer in meshRenderers) {
                minMaxAABB.Encapsulate(meshRenderer.bounds.ToMinMaxAABB());
            }
            var drakes = FindObjectsByType<DrakeMeshRenderer>(FindObjectsSortMode.None);
            foreach (var drake in drakes) {
                minMaxAABB.Encapsulate(drake.WorldBounds);
            }
            return minMaxAABB;
        }

        static Polygon2DAuthoring CreatePolygon(GameObject targetGameObject, MinMaxAABB minMaxAABB) {
            var polygon = targetGameObject.GetComponent<Polygon2DAuthoring>();

            var polygonAccess = new Polygon2DAuthoring.EditorAccess(polygon);
            var points = new Vector2[4];
            points[0] = minMaxAABB.Min.xz;
            points[1] = new Vector2(minMaxAABB.Max.x, minMaxAABB.Min.z);
            points[2] = minMaxAABB.Max.xz;
            points[3] = new Vector2(minMaxAABB.Min.x, minMaxAABB.Max.z);
            polygonAccess.PolygonLocalPoints = points;
            return polygon;
        }
    }
}