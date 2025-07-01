using UnityEngine;
using UnityEditor;
using Pathfinding.Serialization;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Graphs.Grid;
	using Pathfinding.Graphs.Grid.Rules;
	using Pathfinding.Util;

	[CustomGraphEditor(typeof(GridGraph), "Grid Graph")]
	public class GridGraphEditor : GraphEditor {
		[JsonMember]
		public bool locked = true;

		[JsonMember]
		public bool showExtra;

		GraphTransform savedTransform;
		Vector2 savedDimensions;
		float savedNodeSize;

		public bool isMouseDown;

		[JsonMember]
		public GridPivot pivot;

		/// <summary>
		/// Shows the preview for the collision testing options.
		///
		/// [Open online documentation to see images]
		///
		/// On the left you can see a top-down view of the graph with a grid of nodes.
		/// On the right you can see a side view of the graph. The white line at the bottom is the base of the graph, with node positions indicated using small dots.
		/// When using 2D physics, only the top-down view is visible.
		///
		/// The green shape indicates the shape that will be used for collision checking.
		/// </summary>
		[JsonMember]
		public bool collisionPreviewOpen;

		[JsonMember]
		public int selectedTilemap;

		/// <summary>Cached gui style</summary>
		static GUIStyle lockStyle;

		/// <summary>Cached gui style</summary>
		static GUIStyle gridPivotSelectBackground;

		/// <summary>Cached gui style</summary>
		static GUIStyle gridPivotSelectButton;

		public GridGraphEditor() {
        }

        public override void OnInspectorGUI (NavGraph target) {
        }

        bool IsHexagonal(GridGraph graph)
        {
            return default;
        }

        bool IsIsometric(GridGraph graph)
        {
            return default;
        }

        bool IsAdvanced (GridGraph graph) {
            return default;
        }

        InspectorGridMode DetermineGridType(GridGraph graph)
        {
            return default;
        }

        void DrawInspectorMode(GridGraph graph)
        {
        }

        protected virtual void Draw2DMode(GridGraph graph)
        {
        }

        GUIContent[] hexagonSizeContents = {
			new GUIContent("Hexagon Width", "Distance between two opposing sides on the hexagon"),
			new GUIContent("Hexagon Diameter", "Distance between two opposing vertices on the hexagon"),
			new GUIContent("Node Size", "Raw node size value, this doesn't correspond to anything particular on the hexagon."),
		};

		static List<GridLayout> cachedSceneGridLayouts;
		static float cachedSceneGridLayoutsTimestamp = -float.PositiveInfinity;

		static string GetPath (Transform current) {
            return default;
        }

        void DrawTilemapAlignment (GridGraph graph) {
        }

        void DrawFirstSection (GridGraph graph) {
        }

        void DrawRotationField(GridGraph graph)
        {
        }

        void DrawWidthDepthFields(GridGraph graph, out int newWidth, out int newDepth)
        {
            newWidth = default(int);
            newDepth = default(int);
        }

        void DrawIsometricField(GridGraph graph)
        {
        }

        static Vector3 NormalizedPivotPoint(GridGraph graph, GridPivot pivot)
        {
            return default;
        }

        void DrawPositionField(GridGraph graph)
        {
        }

        protected virtual void DrawMiddleSection(GridGraph graph)
        {
        }

        protected virtual void DrawCutCorners(GridGraph graph)
        {
        }

        protected virtual void DrawNeighbours(GridGraph graph)
        {
        }

        protected virtual void DrawMaxClimb(GridGraph graph)
        {
        }

        protected void DrawMaxSlope(GridGraph graph)
        {
        }

        protected void DrawErosion(GridGraph graph)
        {
        }

        void DrawLastSection(GridGraph graph)
        {
        }

        /// <summary>Draws the inspector for a <see cref="GraphCollision"/> class</summary>
        protected virtual void DrawCollisionEditor(GraphCollision collision)
        {
        }

        Vector3[] arcBuffer = new Vector3[21];
        Vector3[] lineBuffer = new Vector3[2];
        void DrawArc(Vector2 center, float radius, float startAngle, float endAngle)
        {
        }

        void DrawLine(Vector2 a, Vector2 b)
        {
        }

        void DrawDashedLine(Vector2 a, Vector2 b, float dashLength)
        {
        }

        static int RoundUpToNextOddNumber(float x)
        {
            return default;
        }

        float interpolatedGridWidthInNodes = -1;
        float lastTime = 0;

		void DrawCollisionPreview (GraphCollision collision) {
        }

        static Dictionary<System.Type, System.Type> ruleEditors;
        static Dictionary<System.Type, string> ruleHeaders;
        static List<System.Type> ruleTypes;
        Dictionary<GridGraphRule, IGridGraphRuleEditor> ruleEditorInstances = new Dictionary<GridGraphRule, IGridGraphRuleEditor>();

		static void FindRuleEditors ()
        {
        }

        IGridGraphRuleEditor GetEditor(GridGraphRule rule)
        {
            return default;
        }

        protected virtual void DrawRules(GridGraph graph)
        {
        }

        public static GridPivot PivotPointSelector(GridPivot pivot)
        {
            return default;
        }

        static readonly Vector3[] handlePoints = new[] { new Vector3(0.0f, 0, 0.5f), new Vector3(1.0f, 0, 0.5f), new Vector3(0.5f, 0, 0.0f), new Vector3(0.5f, 0, 1.0f) };

        public override void OnSceneGUI(NavGraph target)
        {
        }

        public enum GridPivot
        {
            Center,
			TopLeft,
			TopRight,
			BottomLeft,
			BottomRight
		}
	}
}
