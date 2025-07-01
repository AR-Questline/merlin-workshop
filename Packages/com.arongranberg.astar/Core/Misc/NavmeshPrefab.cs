using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding {
	using Pathfinding.Drawing;
	using Pathfinding.Graphs.Navmesh;
	using Pathfinding.Jobs;
	using Pathfinding.Serialization;
	using Pathfinding.Sync;
	using Pathfinding.Pooling;
	using Unity.Jobs;

	/// <summary>
	/// Stores a set of navmesh tiles which can be placed on a recast graph.
	///
	/// This component is used to store chunks of a <see cref="RecastGraph"/> to a file and then be able to efficiently load them and place them on an existing recast graph.
	/// A typical use case is if you have a procedurally generated level consisting of multiple rooms, and scanning the graph after the level has been generated
	/// is too expensive. In this scenario, each room can have its own NavmeshPrefab component which stores the navmesh for just that room, and then when the
	/// level is generated all the NavmeshPrefab components will load their tiles and place them on the recast graph, joining them together at the seams.
	///
	/// Since this component works on tiles, the size of a NavmeshPrefab must be a multiple of the graph's tile size.
	/// The tile size of a recast graph is determined by multiplying the <see cref="RecastGraph.cellSize"/> with the tile size in voxels (<see cref="RecastGraph.editorTileSize"/>).
	/// When a NavmeshPrefab is placed on a recast graph, it will load the tiles into the closest spot (snapping the position and rotation).
	/// The NavmeshPrefab will even resize the graph to make it larger in case you want to place a NavmeshPrefab outside the existing bounds of the graph.
	///
	/// <b>Usage</b>
	///
	/// - Attach a NavmeshPrefab component to a GameObject (typically a prefab) that you want to store the navmesh for.
	/// - Make sure you have a RecastGraph elsewhere in the scene with the same settings that you use for the game.
	/// - Adjust the bounding box to fit your game object. The bounding box should be a multiple of the tile size of the recast graph.
	/// - In the inspector, click the "Scan" button to scan the graph and store the navmesh as a file, referenced by the NavmeshPrefab component.
	/// - Make sure the rendered navmesh looks ok in the scene view.
	/// - In your game, instantiate a prefab with the NavmeshComponent. It will automatically load its stored tiles and place them on the first recast graph in the scene.
	///
	/// If you have multiple recast graphs you may not want it to always use the first recast graph.
	/// In that case you can set the <see cref="applyOnStart"/> field to false and call the <see cref="Apply(RecastGraph)"/> method manually.
	///
	/// <b>Accounting for borders</b>
	///
	/// When scanning a recast graph (and by extension a NavmeshPrefab), a margin is always added around parts of the graph the agent cannot traverse.
	/// This can become problematic when scanning individual chunks separate from the rest of the world, because each one will have a small border of unwalkable space.
	/// The result is that when you place them on a recast graph, they will not be able to connect to each other.
	/// [Open online documentation to see images]
	/// One way to solve this is to scan the prefab together with a mesh that is slightly larger than the prefab, extending the walkable surface enough
	/// so that no border is added. In the image below, this mesh is displayed in white. It can be convenient to make this an invisible collider on the prefab
	/// that is excluded from physics, but is included in the graph's rasterization layer mask.
	/// [Open online documentation to see images]
	/// Now that the border has been removed, the chunks can be placed next to each other and be able to connect.
	/// [Open online documentation to see images]
	///
	/// <b>Loading tiles into a graph</b>
	///
	/// If <see cref="applyOnStart"/> is true, the tiles will be loaded into the first recast graph in the scene when the game starts.
	/// If the recast graph is not scanned, it will be initialized with empty tiles and then the tiles will be loaded into it.
	/// So if your world is made up entirely of NavmeshPrefabs, you can skip scanning for performance by setting A* Inspector -> Settings -> Scan On Awake to false.
	///
	/// You can also apply a NavmeshPrefab to a graph manually by calling the <see cref="Apply(RecastGraph)"/> method.
	///
	/// See: <see cref="RecastGraph"/>
	/// See: <see cref="TileMeshes"/>
	/// </summary>
	[AddComponentMenu("Pathfinding/Navmesh Prefab")]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/navmeshprefab.html")]
	public class NavmeshPrefab : VersionedMonoBehaviour {
		/// <summary>Reference to the serialized tile data</summary>
		public TextAsset serializedNavmesh;

		/// <summary>
		/// If true, the tiles stored in this prefab will be loaded and applied to the first recast graph in the scene when this component is enabled.
		/// If false, you will have to call the <see cref="Apply(RecastGraph)"/> method manually.
		///
		/// If this component is disabled and then enabled again, the tiles will be reloaded.
		/// </summary>
		public bool applyOnStart = true;

		/// <summary>
		/// If true, the tiles that this prefab loaded into the graph will be removed when this component is disabled or destroyed.
		/// If false, the tiles will remain in the graph.
		/// </summary>
		public bool removeTilesWhenDisabled = true;

		/// <summary>
		/// Bounding box for the navmesh to be stored in this prefab.
		/// Should be a multiple of the tile size of the associated recast graph.
		///
		/// See:
		/// See: <see cref="RecastGraph.TileWorldSizeX"/>
		/// </summary>
		public Bounds bounds = new Bounds(Vector3.zero, new Vector3(10, 10, 10));

		bool startHasRun = false;

		protected override void Reset () {
        }

#if UNITY_EDITOR
        public override void DrawGizmos () {
        }
#endif

        /// <summary>
        /// Moves and rotates this object so that it is aligned with tiles in the first recast graph in the scene
        ///
        /// See: SnapToClosestTileAlignment(RecastGraph)
        /// </summary>
        [ContextMenu("Snap to closest tile alignment")]
		public void SnapToClosestTileAlignment () {
        }

        /// <summary>
        /// Applies the navmesh stored in this prefab to the first recast graph in the scene.
        ///
        /// See: <see cref="Apply(RecastGraph)"/> for more details.
        /// </summary>
        [ContextMenu("Apply here")]
		public void Apply () {
        }

        /// <summary>Moves and rotates this object so that it is aligned with tiles in the given graph</summary>
        public void SnapToClosestTileAlignment (RecastGraph graph) {
        }

        /// <summary>
        /// Rounds the size of the <see cref="bounds"/> to the closest multiple of the tile size in the graph, ensuring that the bounds cover at least 1x1 tiles.
        /// The new bounds has the same center and size along the y-axis.
        /// </summary>
        public void SnapSizeToClosestTileMultiple(RecastGraph graph)
        {
        }

        /// <summary>Start is called before the first frame update</summary>
        void Start()
        {
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        /// <summary>
        /// Rounds the size of the bounds to the closest multiple of the tile size in the graph, ensuring that the bounds cover at least 1x1 tiles.
        /// The returned bounds has the same center and size along the y-axis as the input.
        /// </summary>
        public static Bounds SnapSizeToClosestTileMultiple(RecastGraph graph, Bounds bounds) {
            return default;
        }

        public static void SnapToGraph(TileLayout tileLayout, Vector3 position, Quaternion rotation, Bounds bounds, out IntRect tileRect, out int snappedRotation, out float yOffset)
        {
            tileRect = default(IntRect);
            snappedRotation = default(int);
            yOffset = default(float);
        }

        /// <summary>
        /// Applies the navmesh stored in this prefab to the given graph.
        /// The loaded tiles will be placed at the closest valid spot to this object's current position.
        /// Some rounding may occur because the tiles need to be aligned to the graph's tile boundaries.
        ///
        /// If the recast graph is not scanned, it will be initialized with empty tiles and then the tiles in this prefab will be loaded into it.
        ///
        /// If the recast graph is too small and the tiles would have been loaded out of bounds, the graph will first be resized to fit.
        /// If you have a large graph, this resizing can be a somewhat expensive operation.
        ///
        /// See: <see cref="NavmeshPrefab.SnapToClosestTileAlignment()"/>
        /// </summary>
        public void Apply(RecastGraph graph)
        {
        }

        /// <summary>Scans the navmesh using the first recast graph in the scene, and returns a serialized byte representation</summary>
        public byte[] Scan()
        {
            return default;
        }

        /// <summary>Scans the navmesh and returns a serialized byte representation</summary>
        public byte[] Scan(RecastGraph graph)
        {
            return default;
        }

        /// <summary>
        /// Scans the navmesh asynchronously and returns a promise of a byte representation.
        ///
        /// TODO: Maybe change this method to return a <see cref="TileMeshes"/> object instead?
        /// </summary>
        public Promise<SerializedOutput> ScanAsync(RecastGraph graph)
        {
            return default;
        }

        public class SerializedOutput : IProgress, System.IDisposable
        {
            public Promise<TileBuilder.TileBuilderOutput> promise;
            public byte[] data;
            public DisposeArena arena;

            public float Progress => promise.Progress;

            public void Dispose()
            {
            }
        }

		struct SerializeJob : IJob {
			public Promise<TileBuilder.TileBuilderOutput> tileMeshesPromise;
			public SerializedOutput output;

			public void Execute () {
            }
        }

#if UNITY_EDITOR
		/// <summary>
		/// Saves the given data to the <see cref="serializedNavmesh"/> field, or creates a new file if none exists.
		///
		/// A new file will be created if <see cref="serializedNavmesh"/> is null.
		/// If this object is part of a prefab, the file name will be based on the prefab's name.
		///
		/// Warning: This method is only available in the editor.
		///
		/// Warning: You should only pass valid serialized tile data to this function.
		///
		/// See: <see cref="Scan"/>
		/// See: <see cref="ScanAsync"/>
		/// </summary>
		public void SaveToFile (byte[] data) {
        }

        /// <summary>
        /// Scans the navmesh and saves it to the <see cref="serializedNavmesh"/> field.
        /// A new file will be created if <see cref="serializedNavmesh"/> is null.
        /// If this object is part of a prefab, the file name will be based on the prefab's name.
        ///
        /// Note: This method is only available in the editor.
        /// </summary>
        public void ScanAndSaveToFile()
        {
        }
#endif

        protected override void OnUpgradeSerializedData (ref Migrations migrations, bool unityThread) {
        }
    }
}
