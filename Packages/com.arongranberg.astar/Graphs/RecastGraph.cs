using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Mathematics;

namespace Pathfinding {
	using Pathfinding.Serialization;
	using Pathfinding.Graphs.Navmesh;
	using Pathfinding.Util;
	using Pathfinding.Jobs;
	using Pathfinding.Sync;
	using Pathfinding.Pooling;
	using Pathfinding.Graphs.Navmesh.Jobs;
	using Pathfinding.Collections;

	/// <summary>
	/// Automatically generates navmesh graphs based on world geometry.
	///
	/// [Open online documentation to see images]
	///
	/// The recast graph is based on Recast (https://github.com/memononen/recastnavigation).
	/// I have translated a good portion of it to C# to run it natively in Unity.
	///
	/// See: get-started-recast (view in online documentation for working links)
	/// See: graphTypes (view in online documentation for working links)
	///
	/// \section recastinspector Inspector
	///
	/// [Open online documentation to see images]
	///
	/// <b>Shape</b>
	/// \inspectorField{Dimensions, dimensionMode}
	/// \inspectorField{Center, forcedBoundsCenter}
	/// \inspectorField{Size, forcedBoundsSize}
	/// \inspectorField{Rotation, rotation}
	/// \inspectorField{Snap bounds to scene, SnapBoundsToScene}
	///
	/// <b>Input Filtering</b>
	/// \inspectorField{Filter Objects By, collectionSettings.collectionMode}
	/// \inspectorField{Layer Mask, collectionSettings.layerMask}
	/// \inspectorField{Tag Mask, collectionSettings.tagMask}
	/// \inspectorField{Rasterize Terrains, collectionSettings.rasterizeTerrain}
	/// \inspectorField{Rasterize Trees, collectionSettings.rasterizeTrees}
	/// \inspectorField{Heightmap Downsampling, collectionSettings.terrainHeightmapDownsamplingFactor}
	/// \inspectorField{Rasterize Meshes, collectionSettings.rasterizeMeshes}
	/// \inspectorField{Rasterize Colliders, collectionSettings.rasterizeColliders}
	///
	/// <b>Agent Characteristics</b>
	/// \inspectorField{Character Radius, characterRadius}
	/// \inspectorField{Character Height, walkableHeight}
	/// \inspectorField{Max Step Height, walkableClimb}
	/// \inspectorField{Max Slope, maxSlope}
	/// \inspectorField{Per Layer Modifications, perLayerModifications}
	///
	/// <b>Rasterization</b>
	/// \inspectorField{Voxel Size, cellSize}
	/// \inspectorField{Use Tiles, useTiles}
	/// \inspectorField{Tile Size, editorTileSize}
	/// \inspectorField{Max Border Edge Length, maxEdgeLength}
	/// \inspectorField{Edge Simplification, contourMaxError}
	/// \inspectorField{Min Region Size, minRegionSize}
	/// \inspectorField{Round Collider Detail, collectionSettings.colliderRasterizeDetail}
	///
	/// <b>Runtime Settings</b>
	/// \inspectorField{Affected By Navmesh Cuts, enableNavmeshCutting}
	///
	/// <b>Advanced</b>
	/// \inspectorField{Relevant Graph Surface Mode, relevantGraphSurfaceMode}
	/// \inspectorField{Initial Penalty, initialPenalty}
	///
	/// \section howitworks How a recast graph works
	/// When generating a recast graph what happens is that the world is voxelized.
	/// You can think of this as constructing an approximation of the world out of lots of boxes.
	/// If you have played Minecraft it looks very similar (but with smaller boxes).
	/// [Open online documentation to see images]
	///
	/// The Recast process is described as follows:
	/// - The voxel mold is build from the input triangle mesh by rasterizing the triangles into a multi-layer heightfield.
	/// Some simple filters are then applied to the mold to prune out locations where the character would not be able to move.
	/// - The walkable areas described by the mold are divided into simple overlayed 2D regions.
	/// The resulting regions have only one non-overlapping contour, which simplifies the final step of the process tremendously.
	/// - The navigation polygons are peeled off from the regions by first tracing the boundaries and then simplifying them.
	/// The resulting polygons are finally converted to triangles which makes them perfect for pathfinding and spatial reasoning about the level.
	///
	/// The recast generation process usually works directly on the visiable geometry in the world. This is usually a good thing, because world geometry is usually more detailed than the colliders.
	/// You can, however, specify that colliders should be rasterized instead. If you have very detailed world geometry, this can speed up scanning and updating the graph.
	///
	/// \section export Exporting for manual editing
	/// In the editor there is a button for exporting the generated graph to a .obj file.
	/// Usually the generation process is good enough for the game directly, but in some cases you might want to edit some minor details.
	/// So you can export the graph to a .obj file, open it in your favourite 3D application, edit it, and export it to a mesh which Unity can import.
	/// You can then use that mesh in a navmesh graph.
	///
	/// Since many 3D modelling programs use different axis systems (unity uses X=right, Y=up, Z=forward), it can be a bit tricky to get the rotation and scaling right.
	/// For blender for example, what you have to do is to first import the mesh using the .obj importer. Don't change anything related to axes in the settings.
	/// Then select the mesh, open the transform tab (usually the thin toolbar to the right of the 3D view) and set Scale -> Z to -1.
	/// If you transform it using the S (scale) hotkey, it seems to set both Z and Y to -1 for some reason.
	/// Then make the edits you need and export it as an .obj file to somewhere in the Unity project.
	/// But this time, edit the setting named "Forward" to "Z forward" (not -Z as it is per default).
	/// </summary>
	[JsonOptIn]
	[Pathfinding.Util.Preserve]
	public class RecastGraph : NavmeshBase, IUpdatableGraph {
		[JsonMember]
		/// <summary>
		/// Radius of the agent which will traverse the navmesh.
		/// The navmesh will be eroded with this radius.
		///
		/// This value will be rounded up to the nearest multiple of <see cref="cellSize"/>.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public float characterRadius = 0.5F;

		/// <summary>
		/// Max distance from simplified edge to real edge.
		/// This value is measured in voxels. So with the default value of 2 it means that the final navmesh contour may be at most
		/// 2 voxels (i.e 2 times <see cref="cellSize)"/> away from the border that was calculated when voxelizing the world.
		/// A higher value will yield a more simplified and cleaner navmesh while a lower value may capture more details.
		/// However a too low value will cause the individual voxels to be visible (see image below).
		///
		/// [Open online documentation to see images]
		///
		/// See: <see cref="cellSize"/>
		/// </summary>
		[JsonMember]
		public float contourMaxError = 2F;

		/// <summary>
		/// Voxel sample size (x,z).
		/// When generating a recast graph what happens is that the world is voxelized.
		/// You can think of this as constructing an approximation of the world out of lots of boxes.
		/// If you have played Minecraft it looks very similar (but with smaller boxes).
		/// [Open online documentation to see images]
		/// The cell size is the width and depth of those boxes. The height of the boxes is usually much smaller
		/// and automatically calculated, however.
		///
		/// Lower values will yield higher quality navmeshes, however the graph will be slower to scan.
		///
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public float cellSize = 0.25F;

		/// <summary>
		/// Character height.
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public float walkableHeight = 2F;

		/// <summary>
		/// Height the character can climb.
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public float walkableClimb = 0.5F;

		/// <summary>
		/// Max slope in degrees the character can traverse.
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public float maxSlope = 30;

		/// <summary>
		/// Longer edges will be subdivided.
		/// Reducing this value can sometimes improve path quality since similarly sized triangles
		/// yield better paths than really large and really triangles small next to each other.
		/// However it will also add a lot more nodes which will make pathfinding slower.
		/// For more information about this take a look at navmeshnotes (view in online documentation for working links).
		///
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public float maxEdgeLength = 20;

		/// <summary>
		/// Minumum region size.
		/// Small regions will be removed from the navmesh.
		/// Measured in voxels.
		///
		/// [Open online documentation to see images]
		///
		/// If a region is adjacent to a tile border, it will not be removed
		/// even though it is small since the adjacent tile might join it
		/// to form a larger region.
		///
		/// [Open online documentation to see images]
		/// [Open online documentation to see images]
		/// </summary>
		[JsonMember]
		public float minRegionSize = 3;

		/// <summary>
		/// Size in voxels of a single tile.
		/// This is the width of the tile.
		///
		/// [Open online documentation to see images]
		///
		/// A large tile size can be faster to initially scan (but beware of out of memory issues if you try with a too large tile size in a large world)
		/// smaller tile sizes are (much) faster to update.
		///
		/// Different tile sizes can affect the quality of paths. It is often good to split up huge open areas into several tiles for
		/// better quality paths, but too small tiles can also lead to effects looking like invisible obstacles.
		/// For more information about this take a look at navmeshnotes (view in online documentation for working links).
		/// Usually it is best to experiment and see what works best for your game.
		///
		/// When scanning a recast graphs individual tiles can be calculated in parallel which can make it much faster to scan large worlds.
		/// When you want to recalculate a part of a recast graph, this can only be done on a tile-by-tile basis which means that if you often try to update a region
		/// of the recast graph much smaller than the tile size, then you will be doing a lot of unnecessary calculations. However if you on the other hand
		/// update regions of the recast graph that are much larger than the tile size then it may be slower than necessary as there is some overhead in having lots of tiles
		/// instead of a few larger ones (not that much though).
		///
		/// Recommended values are between 64 and 256, but these are very soft limits. It is possible to use both larger and smaller values.
		/// </summary>
		[JsonMember]
		public int editorTileSize = 128;

		/// <summary>
		/// Size of a tile along the X axis in voxels.
		/// \copydetails editorTileSize
		///
		/// Warning: Do not modify, it is set from <see cref="editorTileSize"/> at Scan
		///
		/// See: <see cref="tileSizeZ"/>
		/// </summary>
		[JsonMember]
		public int tileSizeX = 128;

		/// <summary>
		/// Size of a tile along the Z axis in voxels.
		/// \copydetails editorTileSize
		///
		/// Warning: Do not modify, it is set from <see cref="editorTileSize"/> at Scan
		///
		/// See: <see cref="tileSizeX"/>
		/// </summary>
		[JsonMember]
		public int tileSizeZ = 128;


		/// <summary>
		/// If true, divide the graph into tiles, otherwise use a single tile covering the whole graph.
		///
		/// Using tiles is useful for a number of things. But it also has some drawbacks.
		/// - Using tiles allows you to update only a part of the graph at a time. When doing graph updates on a recast graph, it will always recalculate whole tiles (or the whole graph if there are no tiles).
		///    <see cref="NavmeshCut"/> components also work on a tile-by-tile basis.
		/// - Using tiles allows you to use <see cref="NavmeshPrefab"/>s.
		/// - Using tiles can break up very large triangles, which can improve path quality in some cases, and make the navmesh more closely follow the y-coordinates of the ground.
		/// - Using tiles can make it much faster to generate the navmesh, because each tile can be calculated in parallel.
		///    But if the tiles are made too small, then the overhead of having many tiles can make it slower than having fewer tiles.
		/// - Using small tiles can make the path quality worse in some cases, but setting the <see cref="FunnelModifier"/>s quality setting to high (or using <see cref="RichAI.funnelSimplification"/>) will mostly mitigate this.
		///
		/// See: <see cref="editorTileSize"/>
		///
		/// Since: Since 4.1 the default value is true.
		/// </summary>
		[JsonMember]
		public bool useTiles = true;

		/// <summary>
		/// If true, scanning the graph will yield a completely empty graph.
		/// Useful if you want to replace the graph with a custom navmesh for example
		///
		/// Note: This is mostly obsolete now that the <see cref="EnsureInitialized"/> and <see cref="ReplaceTiles"/> functions exist.
		/// </summary>
		public bool scanEmptyGraph;

		public enum RelevantGraphSurfaceMode {
			/// <summary>No RelevantGraphSurface components are required anywhere</summary>
			DoNotRequire,
			/// <summary>
			/// Any surfaces that are completely inside tiles need to have a <see cref="RelevantGraphSurface"/> component
			/// positioned on that surface, otherwise it will be stripped away.
			/// </summary>
			OnlyForCompletelyInsideTile,
			/// <summary>
			/// All surfaces need to have one <see cref="RelevantGraphSurface"/> component
			/// positioned somewhere on the surface and in each tile that it touches, otherwise it will be stripped away.
			/// Only tiles that have a RelevantGraphSurface component for that surface will keep it.
			/// </summary>
			RequireForAll
		}

		/// <summary>Whether to use 3D or 2D mode</summary>
		public enum DimensionMode {
			/// <summary>Allows the recast graph to use 2D colliders</summary>
			Dimension2D,
			/// <summary>Allows the recast graph to use 3D colliders, 3D meshes and terrains</summary>
			Dimension3D,
		}

		/// <summary>
		/// Whether the base of the graph should default to being walkable or unwalkable.
		///
		/// See: <see cref="RecastGraph.backgroundTraversability"/>
		/// </summary>
		public enum BackgroundTraversability {
			/// <summary>Makes the background walkable by default</summary>
			Walkable,
			/// <summary>Makes the background unwalkable by default</summary>
			Unwalkable,
		}

		/// <summary>
		/// Per layer modification settings.
		///
		/// This can be used to make all surfaces with a specific layer get a specific pathfinding tag for example.
		/// Or make all surfaces with a specific layer unwalkable.
		///
		/// See: If you instead want to apply similar settings on an object level, you can use the <see cref="RecastMeshObjStatic"/> component.
		/// </summary>
		[System.Serializable]
		public struct PerLayerModification {
			/// <summary>Unity layer that this modification applies to</summary>
			public int layer;
			/// <summary>\copydocref{RecastNavmeshModifier.mode}</summary>
			public RecastMeshObjStatic.Mode mode;
			/// <summary>\copydocref{RecastNavmeshModifier.surfaceID}</summary>
			public int surfaceID;

			public static PerLayerModification Default => new PerLayerModification {
				layer = 0,
				mode = RecastMeshObjStatic.Mode.WalkableSurface,
				surfaceID = 1,
			};

			public static PerLayerModification[] ToLayerLookup (List<PerLayerModification> perLayerModifications, PerLayerModification defaultValue)
            {
                return default;
            }
        }

        /// <summary>Settings for which meshes/colliders and other objects to include in the graph</summary>
        [System.Serializable]
        public class CollectionSettings
        {
            /// <summary>Determines how the initial filtering of objects is done</summary>
            public enum FilterMode {
				/// <summary>Use a layer mask to filter objects</summary>
				Layers,
				/// <summary>Use tags to filter objects</summary>
				Tags,
			}

			/// <summary>
			/// Determines how the initial filtering of objects is done.
			///
			/// See: <see cref="layerMask"/>
			/// See: <see cref="tagMask"/>
			/// </summary>
			public FilterMode collectionMode = FilterMode.Layers;

			/// <summary>
			/// Objects in all of these layers will be rasterized.
			///
			/// Will only be used if <see cref="collectionMode"/> is set to Layers.
			///
			/// See: <see cref="tagMask"/>
			/// </summary>
			public LayerMask layerMask = -1;

			/// <summary>
			/// Objects tagged with any of these tags will be rasterized.
			///
			/// Will only be used if <see cref="collectionMode"/> is set to Tags.
			///
			/// See: <see cref="layerMask"/>
			/// </summary>
			public List<string> tagMask = new List<string>();

			/// <summary>
			/// Use colliders to calculate the navmesh.
			///
			/// Depending on the <see cref="dimensionMode"/>, either 3D or 2D colliders will be rasterized.
			///
			/// Sphere/Capsule/Circle colliders will be approximated using polygons, with the precision specified in <see cref="colliderRasterizeDetail"/>.
			///
			/// Note: In 2D mode, this is always treated as enabled, because no other types of inputs (like meshes or terrains) are supported.
			/// </summary>
			public bool rasterizeColliders = true;

			/// <summary>
			/// Use scene meshes to calculate the navmesh.
			///
			/// This can get you higher precision than colliders, since colliders are typically very simplified versions of the mesh.
			/// However, it is often slower to scan, and graph updates can be particularly slow.
			///
			/// The reason that graph updates are slower is that there's no efficient way to find all meshes that intersect a given tile,
			/// so the graph has to iterate over all meshes in the scene just to find the ones relevant for the tiles that you want to update.
			/// Colliders, on the other hand, can be efficiently queried using the physics system.
			///
			/// You can disable this and attach a <see cref="RecastMeshObjStatic"/> component (with dynamic=false) to all meshes that you want to be included in the navmesh instead.
			/// That way they will be able to be efficiently queried for, without having to iterate through all meshes in the scene.
			///
			/// In 2D mode, this setting has no effect.
			/// </summary>
			public bool rasterizeMeshes;

			/// <summary>
			/// Use terrains to calculate the navmesh.
			///
			/// In 2D mode, this setting has no effect.
			/// </summary>
			public bool rasterizeTerrain = true;

			/// <summary>
			/// Rasterize tree colliders on terrains.
			///
			/// If the tree prefab has a collider, that collider will be rasterized.
			/// Otherwise a simple box collider will be used and the script will
			/// try to adjust it to the tree's scale, it might not do a very good job though so
			/// an attached collider is preferable.
			///
			/// Note: It seems that Unity will only generate tree colliders at runtime when the game is started.
			/// For this reason, this graph will not pick up tree colliders when scanned outside of play mode
			/// but it will pick them up if the graph is scanned when the game has started. If it still does not pick them up
			/// make sure that the trees actually have colliders attached to them and that the tree prefabs are
			/// in the correct layer (the layer should be included in the layer mask).
			///
			/// In 2D mode, this setting has no effect.
			///
			/// See: <see cref="rasterizeTerrain"/>
			/// See: <see cref="colliderRasterizeDetail"/>
			/// </summary>
			public bool rasterizeTrees = true;

			/// <summary>
			/// Controls how much to downsample the terrain's heightmap before generating the input mesh used for rasterization.
			/// A higher value is faster to scan but less accurate.
			/// </summary>
			public int terrainHeightmapDownsamplingFactor = 3;

			/// <summary>
			/// Controls detail on rasterization of sphere and capsule colliders.
			///
			/// The colliders will be approximated with polygons so that the max distance to the theoretical surface is less than 1/(this number of voxels).
			///
			/// A higher value does not necessarily increase quality of the mesh, but a lower
			/// value will often speed it up.
			///
			/// You should try to keep this value as low as possible without affecting the mesh quality since
			/// that will yield the fastest scan times.
			///
			/// The default value is 1, which corresponds to a maximum error of 1 voxel.
			/// In most cases, increasing this to a value higher than 2 (corresponding to a maximum error of 0.5 voxels) is not useful.
			///
			/// See: rasterizeColliders
			///
			/// Version: Before 4.3.80 this variable was not scaled by the <see cref="cellSize"/>, and so it would not transfer as easily between scenes of different scales.
			/// </summary>
			public float colliderRasterizeDetail = 1;

			/// <summary>
			/// Callback for collecting custom scene meshes.
			///
			/// This callback will be called once when scanning the graph, to allow you to add custom meshes to the graph, and once every time a graph update happens.
			/// Use the <see cref="RecastMeshGatherer"/> class to add meshes that are to be rasterized.
			///
			/// Note: This is a callback, and can therefore not be serialized. You must set this field using code, every time the game starts (and optionally in edit mode as well).
			///
			/// <code>
			/// AstarPath.active.data.recastGraph.collectionSettings.onCollectMeshes += (RecastMeshGatherer gatherer) => {
			///     // Define a mesh using 4 vertices and 2 triangles
			///     var vertices = new Vector3[] {
			///         new Vector3(0, 0, 0),
			///         new Vector3(100, 0, 0),
			///         new Vector3(100, 0, 100),
			///         new Vector3(0, 0, 100)
			///     };
			///     var triangles = new int[] { 0, 1, 2, 0, 2, 3 };
			///     // Register the mesh buffers
			///     var meshDataIndex = gatherer.AddMeshBuffers(vertices, triangles);
			///     // Register the mesh for rasterization
			///     gatherer.AddMesh(new RecastMeshGatherer.GatheredMesh {
			///         meshDataIndex = meshDataIndex,
			///         area = 0,
			///         indexStart = 0,
			///         indexEnd = -1,
			///         bounds = default,
			///         matrix = Matrix4x4.identity,
			///         solid = false,
			///         doubleSided = true,
			///         flatten = false,
			///         areaIsTag = false
			///     });
			/// };
			/// AstarPath.active.Scan();
			/// </code>
			/// </summary>
			public System.Action<RecastMeshGatherer> onCollectMeshes;
		}

		/// <summary>
		/// List of rules that modify the graph based on the layer of the rasterized object.
		///
		/// [Open online documentation to see images]
		///
		/// By default, all layers are treated as walkable surfaces.
		/// But by adding rules to this list, one can for example make all surfaces with a specific layer get a specific pathfinding tag.
		///
		/// Each layer should be modified at most once in this list.
		///
		/// If an object has a <see cref="RecastMeshObjStatic"/> component attached, the settings on that component will override the settings in this list.
		///
		/// See: <see cref="PerLayerModification"/>
		/// </summary>
		[JsonMember]
		public List<PerLayerModification> perLayerModifications = new List<PerLayerModification>();

		/// <summary>
		/// Whether to use 3D or 2D mode.
		///
		/// See: <see cref="DimensionMode"/>
		/// </summary>
		[JsonMember]
		public DimensionMode dimensionMode = DimensionMode.Dimension3D;

		/// <summary>
		/// Whether the base of the graph should default to being walkable or unwalkable.
		///
		/// This is only used in 2D mode. In 3D mode, this setting has no effect.
		///
		/// For 2D games, it can be very useful to set the background to be walkable by default, and then
		/// constrain walkability using colliders.
		///
		/// If you don't want to use a walkable background, you can instead create colliders and attach a RecastNavmeshModifier with Surface Type set to Walkable Surface.
		/// These will then create walkable regions.
		///
		/// See: <see cref="dimensionMode"/>
		/// </summary>
		[JsonMember]
		public BackgroundTraversability backgroundTraversability = BackgroundTraversability.Walkable;

		/// <summary>
		/// Require every region to have a RelevantGraphSurface component inside it.
		/// A RelevantGraphSurface component placed in the scene specifies that
		/// the navmesh region it is inside should be included in the navmesh.
		///
		/// If this is set to OnlyForCompletelyInsideTile
		/// a navmesh region is included in the navmesh if it
		/// has a RelevantGraphSurface inside it, or if it
		/// is adjacent to a tile border. This can leave some small regions
		/// which you didn't want to have included because they are adjacent
		/// to tile borders, but it removes the need to place a component
		/// in every single tile, which can be tedious (see below).
		///
		/// If this is set to RequireForAll
		/// a navmesh region is included only if it has a RelevantGraphSurface
		/// inside it. Note that even though the navmesh
		/// looks continous between tiles, the tiles are computed individually
		/// and therefore you need a RelevantGraphSurface component for each
		/// region and for each tile.
		///
		/// [Open online documentation to see images]
		/// In the above image, the mode OnlyForCompletelyInsideTile was used. Tile borders
		/// are highlighted in black. Note that since all regions are adjacent to a tile border,
		/// this mode didn't remove anything in this case and would give the same result as DoNotRequire.
		/// The RelevantGraphSurface component is shown using the green gizmo in the top-right of the blue plane.
		///
		/// [Open online documentation to see images]
		/// In the above image, the mode RequireForAll was used. No tiles were used.
		/// Note that the small region at the top of the orange cube is now gone, since it was not the in the same
		/// region as the relevant graph surface component.
		/// The result would have been identical with OnlyForCompletelyInsideTile since there are no tiles (or a single tile, depending on how you look at it).
		///
		/// [Open online documentation to see images]
		/// The mode RequireForAll was used here. Since there is only a single RelevantGraphSurface component, only the region
		/// it was in, in the tile it is placed in, will be enabled. If there would have been several RelevantGraphSurface in other tiles,
		/// those regions could have been enabled as well.
		///
		/// [Open online documentation to see images]
		/// Here another tile size was used along with the OnlyForCompletelyInsideTile.
		/// Note that the region on top of the orange cube is gone now since the region borders do not intersect that region (and there is no
		/// RelevantGraphSurface component inside it).
		///
		/// Note: When not using tiles. OnlyForCompletelyInsideTile is equivalent to RequireForAll.
		/// </summary>
		[JsonMember]
		public RelevantGraphSurfaceMode relevantGraphSurfaceMode = RelevantGraphSurfaceMode.DoNotRequire;

		/// <summary>
		/// Determines which objects are used to build the graph, when it is scanned.
		///
		/// Also contains some settings for how to convert objects into meshes.
		/// Spherical colliders, for example, need to be converted into a triangular mesh before they can be used in the graph.
		///
		/// See: <see cref="CollectionSettings"/>
		/// </summary>
		[JsonMember]
		public CollectionSettings collectionSettings = new CollectionSettings();

		/// <summary>
		/// Use colliders to calculate the navmesh.
		///
		/// Depending on the <see cref="dimensionMode"/>, either 3D or 2D colliders will be rasterized.
		///
		/// Sphere/Capsule/Circle colliders will be approximated using polygons, with the precision specified in <see cref="colliderRasterizeDetail"/>.
		/// Deprecated: Use <see cref="collectionSettings.rasterizeColliders"/> instead
		/// </summary>
		[System.Obsolete("Use collectionSettings.rasterizeColliders instead")]
		public bool rasterizeColliders {
			get => collectionSettings.rasterizeColliders;
			set => collectionSettings.rasterizeColliders = value;
		}

		/// <summary>
		/// Use scene meshes to calculate the navmesh.
		///
		/// This can get you higher precision than colliders, since colliders are typically very simplified versions of the mesh.
		/// However, it is often slower to scan, and graph updates can be particularly slow.
		///
		/// The reason that graph updates are slower is that there's no efficient way to find all meshes that intersect a given tile,
		/// so the graph has to iterate over all meshes in the scene just to find the ones relevant for the tiles that you want to update.
		/// Colliders, on the other hand, can be efficiently queried using the physics system.
		///
		/// You can disable this and attach a <see cref="RecastMeshObjStatic"/> component (with dynamic=false) to all meshes that you want to be included in the navmesh instead.
		/// That way they will be able to be efficiently queried for, without having to iterate through all meshes in the scene.
		///
		/// In 2D mode, this setting has no effect.
		/// Deprecated: Use <see cref="collectionSettings.rasterizeMeshes"/> instead
		/// </summary>
		[System.Obsolete("Use collectionSettings.rasterizeMeshes instead")]
		public bool rasterizeMeshes {
			get => collectionSettings.rasterizeMeshes;
			set => collectionSettings.rasterizeMeshes = value;
		}

		/// <summary>
		/// Use terrains to calculate the navmesh.
		///
		/// In 2D mode, this setting has no effect.
		/// Deprecated: Use <see cref="collectionSettings.rasterizeTerrain"/> instead
		/// </summary>
		[System.Obsolete("Use collectionSettings.rasterizeTerrain instead")]
		public bool rasterizeTerrain {
			get => collectionSettings.rasterizeTerrain;
			set => collectionSettings.rasterizeTerrain = value;
		}

		/// <summary>
		/// Rasterize tree colliders on terrains.
		///
		/// If the tree prefab has a collider, that collider will be rasterized.
		/// Otherwise a simple box collider will be used and the script will
		/// try to adjust it to the tree's scale, it might not do a very good job though so
		/// an attached collider is preferable.
		///
		/// Note: It seems that Unity will only generate tree colliders at runtime when the game is started.
		/// For this reason, this graph will not pick up tree colliders when scanned outside of play mode
		/// but it will pick them up if the graph is scanned when the game has started. If it still does not pick them up
		/// make sure that the trees actually have colliders attached to them and that the tree prefabs are
		/// in the correct layer (the layer should be included in the layer mask).
		///
		/// In 2D mode, this setting has no effect.
		///
		/// See: <see cref="rasterizeTerrain"/>
		/// See: <see cref="colliderRasterizeDetail"/>
		/// Deprecated: Use <see cref="collectionSettings.rasterizeTrees"/> instead
		/// </summary>
		[System.Obsolete("Use collectionSettings.rasterizeTrees instead")]
		public bool rasterizeTrees {
			get => collectionSettings.rasterizeTrees;
			set => collectionSettings.rasterizeTrees = value;
		}

		/// <summary>
		/// Controls detail on rasterization of sphere and capsule colliders.
		///
		/// The colliders will be approximated with polygons so that the max distance to the theoretical surface is less than 1/(this number of voxels).
		///
		/// A higher value does not necessarily increase quality of the mesh, but a lower
		/// value will often speed it up.
		///
		/// You should try to keep this value as low as possible without affecting the mesh quality since
		/// that will yield the fastest scan times.
		///
		/// The default value is 1, which corresponds to a maximum error of 1 voxel.
		/// In most cases, increasing this to a value higher than 2 (corresponding to a maximum error of 0.5 voxels) is not useful.
		///
		/// See: rasterizeColliders
		///
		/// Version: Before 4.3.80 this variable was not scaled by the <see cref="cellSize"/>, and so it would not transfer as easily between scenes of different scales.
		///
		/// Deprecated: Use <see cref="collectionSettings.colliderRasterizeDetail"/> instead
		/// </summary>
		[System.Obsolete("Use collectionSettings.colliderRasterizeDetail instead")]
		public float colliderRasterizeDetail {
			get => collectionSettings.colliderRasterizeDetail;
			set => collectionSettings.colliderRasterizeDetail = value;
		}

		/// <summary>
		/// Layer mask which filters which objects to include.
		/// See: <see cref="tagMask"/>
		/// Deprecated: Use <see cref="collectionSettings.layerMask"/> instead
		/// </summary>
		[System.Obsolete("Use collectionSettings.layerMask instead")]
		public LayerMask mask {
			get => collectionSettings.layerMask;
			set => collectionSettings.layerMask = value;
		}

		/// <summary>
		/// Objects tagged with any of these tags will be rasterized.
		/// Note that this extends the layer mask, so if you only want to use tags, set <see cref="mask"/> to 'Nothing'.
		///
		/// See: <see cref="mask"/>
		/// Deprecated: Use <see cref="collectionSettings.tagMask"/> instead
		/// </summary>
		[System.Obsolete("Use collectionSettings.tagMask instead")]
		public List<string> tagMask {
			get => collectionSettings.tagMask;
			set => collectionSettings.tagMask = value;
		}

		/// <summary>
		/// Controls how large the sample size for the terrain is.
		/// A higher value is faster to scan but less accurate.
		///
		/// The heightmap resolution is effectively divided by this value, before the terrain is rasterized.
		///
		/// Deprecated: Use <see cref="collectionSettings.terrainHeightmapDownsamplingFactor"/> instead
		/// </summary>
		[System.Obsolete("Use collectionSettings.terrainHeightmapDownsamplingFactor instead")]
		public int terrainSampleSize {
			get => collectionSettings.terrainHeightmapDownsamplingFactor;
			set => collectionSettings.terrainHeightmapDownsamplingFactor = value;
		}

		/// <summary>Rotation of the graph in degrees</summary>
		[JsonMember]
		public Vector3 rotation;

		/// <summary>
		/// Center of the bounding box.
		/// Scanning will only be done inside the bounding box
		/// </summary>
		[JsonMember]
		public Vector3 forcedBoundsCenter;

#if UNITY_EDITOR
		/// <summary>Internal field used to warn users when the mesh includes meshes that are not readable at runtime</summary>
		public List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime;
#endif

		public override float NavmeshCuttingCharacterRadius => characterRadius;

		public override bool RecalculateNormals { get { return true; } }

		public override float TileWorldSizeX {
			get {
				return tileSizeX*cellSize;
			}
		}

		public override float TileWorldSizeZ {
			get {
				return tileSizeZ*cellSize;
			}
		}

		public override float MaxTileConnectionEdgeDistance {
			get {
				return walkableClimb;
			}
		}

		/// <summary>
		/// World bounding box for the graph.
		///
		/// This always contains the whole graph.
		///
		/// Note: Since this is an axis-aligned bounding box, it may not be particularly tight if the graph is significantly rotated.
		///
		/// [Open online documentation to see images]
		/// </summary>
		public override Bounds bounds {
			get {
				var m = (float4x4)CalculateTransform().matrix;
				var b = new ToWorldMatrix(new float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz)).ToWorld(new Bounds(Vector3.zero, forcedBoundsSize));
				b.center += forcedBoundsCenter;
				return b;
			}
		}

		/// <summary>
		/// True if the point is inside the bounding box of this graph.
		///
		/// Note: This method uses a tighter non-axis-aligned bounding box than you can get from the <see cref="bounds"/> property.
		///
		/// Note: What is considered inside the bounds is only updated when the graph is scanned. For an unscanned graph, this will always return false.
		///
		/// In 2D mode, the point is considered inside if it is contained along the graph's X and Z axes (Y is ignored).
		/// Note that the graph's X and Z axes are typically aligned with the world's X and Y axes when using 2D mode.
		/// </summary>
		public override bool IsInsideBounds (Vector3 point)
        {
            return default;
        }

        [System.Obsolete("Use SnapBoundsToScene instead")]
        public void SnapForceBoundsToScene()
        {
        }

        /// <summary>
        /// Changes the bounds of the graph to precisely encapsulate all objects in the scene.
        /// The bounds will be expanded to fit all objects in the scene which match the current scanning settings.
        ///
        /// This method corresponds to the 'Snap bounds to scene' button in the inspector.
        ///
        /// See: rasterizeMeshes
        /// See: rasterizeTerrain
        /// See: rasterizeColliders
        /// See: mask
        /// See: tagMask
        ///
        /// See: forcedBoundsCenter
        /// See: forcedBoundsSize
        /// </summary>
        public void SnapBoundsToScene()
        {
        }

        DisposeArena pendingGraphUpdateArena = new DisposeArena();

        class RecastGraphUpdatePromise : IGraphUpdatePromise
        {
            public List<(Promise<TileBuilder.TileBuilderOutput>, Promise<TileCutter.TileCutterOutput>, Promise<JobBuildNodes.BuildNodeTilesOutput>)> promises;
			public List<GraphUpdateObject> graphUpdates;
			public RecastGraph graph;
			int graphHash;

			public RecastGraphUpdatePromise (RecastGraph graph, List<GraphUpdateObject> graphUpdates)
            {
            }

            public IEnumerator<JobHandle> Prepare()
            {
                return default;
            }

            static int HashSettings(RecastGraph graph) => (((graph.tileXCount * 31) ^ graph.tileZCount) * 31 ^ graph.TileWorldSizeX.GetHashCode() * 31) ^ graph.TileWorldSizeZ.GetHashCode();

            public void Apply(IGraphUpdateContext ctx)
            {
            }
        }

        IGraphUpdatePromise IUpdatableGraph.ScheduleGraphUpdates(List<GraphUpdateObject> graphUpdates) => new RecastGraphUpdatePromise(this, graphUpdates);

        class RecastGraphScanPromise : IGraphUpdatePromise
        {
            public RecastGraph graph;
            TileLayout tileLayout;
            bool emptyGraph;
            NavmeshTile[] tiles;
            IProgress progressSource;
            NavmeshUpdates.NavmeshUpdateSettings cutSettings;

            public float Progress => progressSource != null ? progressSource.Progress : 1;

#if UNITY_EDITOR
            List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime;
#endif

            public IEnumerator<JobHandle> Prepare () {
                return default;
            }

            public void Apply(IGraphUpdateContext ctx)
            {
            }
        }

        /// <summary>
        /// Moves the recast graph by a number of tiles, discarding old tiles and scanning new ones.
        ///
        /// Note: Only translation in a single direction is supported. dx == 0 || dz == 0 must hold.
        /// If you need to move the graph diagonally, then you can call this function twice, once for each axis.
        ///
        /// This is used by the <see cref="ProceduralGraphMover"/> component to efficiently move the graph.
        ///
        /// All tiles that can stay in the same position will stay. The ones that would have fallen off the edge of the graph will be discarded,
        /// and new tiles will be created and scanned at the other side of the graph.
        ///
        /// See: <see cref="ProceduralGraphMover"/>
        ///
        /// Returns: An async graph update promise. See <see cref="IGraphUpdatePromise"/>
        /// </summary>
        /// <param name="dx">Number of tiles along the graph's X axis to move by.</param>
        /// <param name="dz">Number of tiles along the graph's Z axis to move by.</param>
        public IGraphUpdatePromise TranslateInDirection (int dx, int dz) {
            return default;
        }

        bool hasExtendedInX = false;
		bool hasExtendedInZ = false;

		class RecastMovePromise : IGraphUpdatePromise {
			RecastGraph graph;
			TileMeshes tileMeshes;
			Vector2Int delta;
			IntRect newTileRect;

			public RecastMovePromise(RecastGraph graph, Vector2Int delta) {
            }

            public IEnumerator<JobHandle> Prepare()
            {
                return default;
            }

            public void Apply (IGraphUpdateContext ctx) {
            }
        }

		protected override IGraphUpdatePromise ScanInternal (bool async) => new RecastGraphScanPromise { graph = this };

		public override GraphTransform CalculateTransform () {
            return default;
        }

        public static GraphTransform CalculateTransform (Bounds bounds, Quaternion rotation) {
            return default;
        }

        protected void SetLayout (TileLayout info) {
        }

        /// <summary>Convert character radius to a number of voxels</summary>
        internal int CharacterRadiusInVoxels {
			get {
				// Round it up most of the time, but round it down
				// if it is very close to the result when rounded down
				return Mathf.CeilToInt((characterRadius / cellSize) - 0.1f);
			}
		}

		/// <summary>
		/// Number of extra voxels on each side of a tile to ensure accurate navmeshes near the tile border.
		/// The width of a tile is expanded by 2 times this value (1x to the left and 1x to the right)
		/// </summary>
		internal int TileBorderSizeInVoxels {
			get {
				return CharacterRadiusInVoxels + 3;
			}
		}

		internal float TileBorderSizeInWorldUnits {
			get {
				return TileBorderSizeInVoxels*cellSize;
			}
		}

		/// <summary>
		/// Resize the number of tiles that this graph contains.
		///
		/// This can be used both to make a graph larger, smaller or move the bounds of the graph around.
		/// The new bounds are relative to the existing bounds which are IntRect(0, 0, tileCountX-1, tileCountZ-1).
		///
		/// Any current tiles that fall outside the new bounds will be removed.
		/// Any new tiles that did not exist inside the previous bounds will be created as empty tiles.
		/// All other tiles will be preserved. They will stay at their current world space positions.
		///
		/// Note: This is intended to be used at runtime on an already scanned graph.
		/// If you want to change the bounding box of a graph like in the editor, use <see cref="forcedBoundsSize"/> and <see cref="forcedBoundsCenter"/> instead.
		///
		/// <code>
		/// AstarPath.active.AddWorkItem(() => {
		///     var graph = AstarPath.active.data.recastGraph;
		///     var currentBounds = new IntRect(0, 0, graph.tileXCount-1, graph.tileZCount-1);
		///
		///     // Make the graph twice as large, but discard the first 3 columns.
		///     // All other tiles will be kept and stay at the same position in the world.
		///     // The new tiles will be empty.
		///     graph.Resize(new IntRect(3, 0, currentBounds.xmax*2, currentBounds.ymax*2));
		/// });
		/// </code>
		/// </summary>
		/// <param name="newTileBounds">Rectangle of tiles that the graph should contain. Relative to the old bounds.</param>
		public virtual void Resize (IntRect newTileBounds) {
        }

        /// <summary>Initialize the graph with empty tiles if it is not currently scanned</summary>
        public void EnsureInitialized()
        {
        }

        public void DestroyAllNodesFast()
        {
        }

        /// <summary>
        /// Load tiles from a <see cref="TileMeshes"/> object into this graph.
        ///
        /// This can be used for many things, for example world streaming or placing large prefabs that have been pre-scanned.
        ///
        /// The loaded tiles must have the same world-space size as this graph's tiles.
        /// The world-space size for a recast graph is given by the <see cref="cellSize"/> multiplied by <see cref="tileSizeX"/> (or <see cref="tileSizeZ)"/>.
        ///
        /// If the graph is not scanned when this method is called, the graph will be initialized and consist of just the tiles loaded by this call.
        ///
        /// <code>
        /// // Scans the first 6x6 chunk of tiles of the recast graph (the IntRect uses inclusive coordinates)
        /// var graph = AstarPath.active.data.recastGraph;
        /// var buildSettings = RecastBuilder.BuildTileMeshes(graph, new TileLayout(graph), new IntRect(0, 0, 5, 5));
        /// var disposeArena = new Pathfinding.Jobs.DisposeArena();
        /// var promise = buildSettings.Schedule(disposeArena);
        ///
        /// AstarPath.active.AddWorkItem(() => {
        ///     // Block until the asynchronous job completes
        ///     var result = promise.Complete();
        ///     TileMeshes tiles = result.tileMeshes.ToManaged();
        ///     // Take the scanned tiles and place them in the graph,
        ///     // but not at their original location, but 2 tiles away, rotated 90 degrees.
        ///     tiles.tileRect = tiles.tileRect.Offset(new Vector2Int(2, 0));
        ///     tiles.Rotate(1);
        ///     graph.ReplaceTiles(tiles);
        ///
        ///     // Dispose unmanaged data
        ///     disposeArena.DisposeAll();
        ///     result.Dispose();
        /// });
        /// </code>
        ///
        /// See: <see cref="NavmeshPrefab"/>
        /// See: <see cref="TileMeshes"/>
        /// See: <see cref="RecastBuilder.BuildTileMeshes"/>
        /// See: <see cref="Resize"/>
        /// See: <see cref="ReplaceTile"/>
        /// See: <see cref="TileWorldSizeX"/>
        /// See: <see cref="TileWorldSizeZ"/>
        /// </summary>
        /// <param name="tileMeshes">The tiles to load. They will be loaded into the graph at the \reflink{TileMeshes.tileRect} tile coordinates.</param>
        /// <param name="yOffset">All vertices in the loaded tiles will be moved upwards (or downwards if negative) by this amount.</param>
        public void ReplaceTiles(TileMeshes tileMeshes, float yOffset = 0)
        {
        }

        protected override void PostDeserialization(GraphSerializationContext ctx)
        {
        }

        protected override void DestroyAllNodes()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RemoveAllNodesCrossGraphConnections()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RemoveCrossGraphConnections(TriangleMeshNode meshNode)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CallDestroyOnAllNodes()
        {
        }
    }
}
