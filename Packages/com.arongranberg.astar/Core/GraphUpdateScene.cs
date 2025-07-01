using UnityEngine;

namespace Pathfinding {
	using Pathfinding.Drawing;

	[AddComponentMenu("Pathfinding/GraphUpdateScene")]
	/// <summary>
	/// Helper class for easily updating graphs.
	///
	/// To use the GraphUpdateScene component, create a new empty GameObject and add the component to it, it can be found under Components-->Pathfinding-->GraphUpdateScene.
	///
	/// The region which the component will affect is defined by creating a polygon in the scene.
	/// If you make sure you have the Position tool enabled (top-left corner of the Unity window) you can shift+click in the scene view to add more points to the polygon.
	/// You can remove points using shift+alt+click.
	/// By clicking on the points you can bring up a positioning tool. You can also open the "points" array in the inspector to set each point's coordinates manually.
	/// [Open online documentation to see images]
	/// In the inspector there are a number of variables. The first one is named "Convex", it sets if the convex hull of the points should be calculated or if the polygon should be used as-is.
	/// Using the convex hull is faster when applying the changes to the graph, but with a non-convex polygon you can specify more complicated areas.
	/// The next two variables, called "Apply On Start" and "Apply On Scan" determine when to apply the changes. If the object is in the scene from the beginning, both can be left on, it doesn't
	/// matter since the graph is also scanned at start. However if you instantiate it later in the game, you can make it apply it's setting directly, or wait until the next scan (if any).
	/// If the graph is rescanned, all GraphUpdateScene components which have the Apply On Scan variable toggled will apply their settings again to the graph since rescanning clears all previous changes.
	/// You can also make it apply it's changes using scripting.
	/// <code> GetComponent<GraphUpdateScene>().Apply (); </code>
	/// The above code will make it apply its changes to the graph (assuming a GraphUpdateScene component is attached to the same GameObject).
	///
	/// Next there is "Modify Walkability" and "Set Walkability" (which appears when "Modify Walkability" is toggled).
	/// If Modify Walkability is set, then all nodes inside the area will either be set to walkable or unwalkable depending on the value of the "Set Walkability" variable.
	///
	/// Penalty can also be applied to the nodes. A higher penalty (aka weight) makes the nodes harder to traverse so it will try to avoid those areas.
	///
	/// The tagging variables can be read more about on this page: tags (view in online documentation for working links) "Working with tags".
	///
	/// Note: The Y (up) axis of the transform that this component is attached to should be in the same direction as the up direction of the graph.
	/// So if you for example have a grid in the XY plane then the transform should have the rotation (-90,0,0).
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/graphupdatescene.html")]
	public class GraphUpdateScene : GraphModifier {
		/// <summary>Points which define the region to update</summary>
		public Vector3[] points;

		/// <summary>Private cached convex hull of the <see cref="points"/></summary>
		private Vector3[] convexPoints;

		/// <summary>
		/// Use the convex hull of the points instead of the original polygon.
		///
		/// See: https://en.wikipedia.org/wiki/Convex_hull
		/// </summary>
		public bool convex = true;

		/// <summary>
		/// Minumum height of the bounds of the resulting Graph Update Object.
		/// Useful when all points are laid out on a plane but you still need a bounds with a height greater than zero since a
		/// zero height graph update object would usually result in no nodes being updated.
		/// </summary>
		public float minBoundsHeight = 1;

		/// <summary>
		/// Penalty to add to nodes.
		/// Usually you need quite large values, at least 1000-10000. A higher penalty means that agents will try to avoid those nodes more.
		///
		/// Be careful when setting negative values since if a node gets a negative penalty it will underflow and instead get
		/// really large. In most cases a warning will be logged if that happens.
		///
		/// See: tags (view in online documentation for working links) for another way of applying penalties.
		/// </summary>
		public int penaltyDelta;

		/// <summary>If true, then all affected nodes will be made walkable or unwalkable according to <see cref="setWalkability"/></summary>
		public bool modifyWalkability;

		/// <summary>Nodes will be made walkable or unwalkable according to this value if <see cref="modifyWalkability"/> is true</summary>
		public bool setWalkability;

		/// <summary>Apply this graph update object on start</summary>
		public bool applyOnStart = true;

		/// <summary>Apply this graph update object whenever a graph is rescanned</summary>
		public bool applyOnScan = true;

		/// <summary>
		/// Update node's walkability and connectivity using physics functions.
		/// For grid graphs, this will update the node's position and walkability exactly like when doing a scan of the graph.
		/// If enabled for grid graphs, <see cref="modifyWalkability"/> will be ignored.
		///
		/// For Point Graphs, this will recalculate all connections which passes through the bounds of the resulting Graph Update Object
		/// using raycasts (if enabled).
		/// </summary>
		public bool updatePhysics;

		/// <summary>\copydoc Pathfinding::GraphUpdateObject::resetPenaltyOnPhysics</summary>
		public bool resetPenaltyOnPhysics = true;

		/// <summary>\copydoc Pathfinding::GraphUpdateObject::updateErosion</summary>
		public bool updateErosion = true;

		/// <summary>
		/// Should the tags of the nodes be modified.
		/// If enabled, set all nodes' tags to <see cref="setTag"/>
		/// </summary>
		public bool modifyTag;

		/// <summary>If <see cref="modifyTag"/> is enabled, set all nodes' tags to this value</summary>
		public PathfindingTag setTag;

		/// <summary>Emulates behavior from before version 4.0</summary>
		[HideInInspector]
		public bool legacyMode = false;

		/// <summary>
		/// Private cached inversion of <see cref="setTag"/>.
		/// Used for InvertSettings()
		/// </summary>
		private PathfindingTag setTagInvert;

		/// <summary>
		/// Has apply been called yet.
		/// Used to prevent applying twice when both applyOnScan and applyOnStart are enabled
		/// </summary>
		private bool firstApplied;

		/// <summary>
		/// Use world space for coordinates.
		/// If true, the shape will not follow when moving around the transform.
		/// </summary>
		[SerializeField]
		[UnityEngine.Serialization.FormerlySerializedAs("useWorldSpace")]
		private bool legacyUseWorldSpace;

		[SerializeField]
		[UnityEngine.Serialization.FormerlySerializedAs("setTag")]
		private int setTagCompatibility = -1;

		/// <summary>Do some stuff at start</summary>
		public void Start () {
        }

        public override void OnPostScan () {
        }

        /// <summary>
        /// Inverts all invertable settings for this GUS.
        /// Namely: penalty delta, walkability, tags.
        ///
        /// Penalty delta will be changed to negative penalty delta.
        /// <see cref="setWalkability"/> will be inverted.
        /// <see cref="setTag"/> will be stored in a private variable, and the new value will be 0. When calling this function again, the saved
        /// value will be the new value.
        ///
        /// Calling this function an even number of times without changing any settings in between will be identical to no change in settings.
        /// </summary>
        public virtual void InvertSettings () {
        }

        /// <summary>
        /// Recalculate convex hull.
        /// Will not do anything if <see cref="convex"/> is disabled.
        /// </summary>
        public void RecalcConvex () {
        }

        /// <summary>
        /// Calculates the bounds for this component.
        /// This is a relatively expensive operation, it needs to go through all points and
        /// run matrix multiplications.
        /// </summary>
        public Bounds GetBounds () {
            return default;
        }

        /// <summary>
        /// The GraphUpdateObject which would be applied by this component.
        ///
        /// No graphs are actually updated by this function. Call AstarPath.active.UpdateGraphs and pass this object if you want that.
        /// This method is useful if you want to modify the object before passing it to the UpdateGraphs function.
        ///
        /// See: <see cref="Apply"/>
        /// </summary>
        public virtual GraphUpdateObject GetGraphUpdate () {
            return default;
        }

        /// <summary>
        /// Updates graphs with a created GUO.
        /// Creates a Pathfinding.GraphUpdateObject with a Pathfinding.GraphUpdateShape
        /// representing the polygon of this object and update all graphs using AstarPath.UpdateGraphs.
        /// This will not update graphs immediately. See AstarPath.UpdateGraph for more info.
        /// </summary>
        public void Apply()
        {
        }

        static readonly Color GizmoColorSelected = new Color(227 / 255f, 61 / 255f, 22 / 255f, 1.0f);
        static readonly Color GizmoColorUnselected = new Color(227 / 255f, 61 / 255f, 22 / 255f, 0.9f);

        /// <summary>Draws some gizmos</summary>
        public override void DrawGizmos()
        {
        }

        /// <summary>
        /// Disables legacy mode if it is enabled.
        /// Legacy mode is automatically enabled for components when upgrading from an earlier version than 3.8.6.
        /// </summary>
        public void DisableLegacyMode () {
        }

        protected override void OnUpgradeSerializedData (ref Serialization.Migrations migrations, bool unityThread) {
        }
    }
}
