using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	using UnityEngine.Profiling;
	using Pathfinding.Util;
	using Pathfinding.Serialization;
	using Unity.Collections;
	using Unity.Jobs;
	using Pathfinding.Graphs.Navmesh.Jobs;
	using Pathfinding.Graphs.Navmesh;
	using Unity.Mathematics;

	/// <summary>
	/// Generates graphs based on navmeshes.
	/// [Open online documentation to see images]
	///
	/// Navmeshes are meshes in which each triangle defines a walkable area.
	/// These are great because the AI can get so much more information on how it can walk.
	/// Polygons instead of points mean that the <see cref="FunnelModifier"/> can produce really nice looking paths, and the graphs are also really fast to search
	/// and have a low memory footprint because fewer nodes are usually needed to describe the same area compared to grid graphs.
	///
	/// The navmesh graph requires that you create a navmesh manually. The package also has support for generating navmeshes automatically using the <see cref="RecastGraph"/>.
	///
	/// For a tutorial on how to configure a navmesh graph, take a look at getstarted2 (view in online documentation for working links).
	///
	/// [Open online documentation to see images]
	///
	/// \section navmeshgraph-inspector Inspector
	/// [Open online documentation to see images]
	///
	/// \inspectorField{Source Mesh, sourceMesh}
	/// \inspectorField{Offset, offset}
	/// \inspectorField{Rotation, rotation}
	/// \inspectorField{Scale, scale}
	/// \inspectorField{Recalculate Normals, recalculateNormals}
	/// \inspectorField{Affected By Navmesh Cuts, enableNavmeshCutting}
	/// \inspectorField{Agent Radius, navmeshCuttingCharacterRadius}
	/// \inspectorField{Initial Penalty, initialPenalty}
	///
	/// See: <see cref="RecastGraph"/>
	/// </summary>
	[JsonOptIn]
	[Pathfinding.Util.Preserve]
	public class NavMeshGraph : NavmeshBase, IUpdatableGraph {
		/// <summary>Mesh to construct navmesh from</summary>
		[JsonMember]
		public Mesh sourceMesh;

		/// <summary>Offset in world space</summary>
		[JsonMember]
		public Vector3 offset;

		/// <summary>Rotation in degrees</summary>
		[JsonMember]
		public Vector3 rotation;

		/// <summary>Scale of the graph</summary>
		[JsonMember]
		public float scale = 1;

		/// <summary>
		/// Determines how normals are calculated.
		/// Disable for spherical graphs or other complicated surfaces that allow the agents to e.g walk on walls or ceilings.
		///
		/// By default the normals of the mesh will be flipped so that they point as much as possible in the upwards direction.
		/// The normals are important when connecting adjacent nodes. Two adjacent nodes will only be connected if they are oriented the same way.
		/// This is particularly important if you have a navmesh on the walls or even on the ceiling of a room. Or if you are trying to make a spherical navmesh.
		/// If you do one of those things then you should set disable this setting and make sure the normals in your source mesh are properly set.
		///
		/// If you for example take a look at the image below. In the upper case then the nodes on the bottom half of the
		/// mesh haven't been connected with the nodes on the upper half because the normals on the lower half will have been
		/// modified to point inwards (as that is the direction that makes them face upwards the most) while the normals on
		/// the upper half point outwards. This causes the nodes to not connect properly along the seam. When this option
		/// is set to false instead the nodes are connected properly as in the original mesh all normals point outwards.
		/// [Open online documentation to see images]
		///
		/// The default value of this field is true to reduce the risk for errors in the common case. If a mesh is supplied that
		/// has all normals pointing downwards and this option is false, then some methods like <see cref="PointOnNavmesh"/> will not work correctly
		/// as they assume that the normals point upwards. For a more complicated surface like a spherical graph those methods make no sense anyway
		/// as there is no clear definition of what it means to be "inside" a triangle when there is no clear up direction.
		/// </summary>
		[JsonMember]
		public bool recalculateNormals = true;

		/// <summary>
		/// Cached bounding box minimum of <see cref="sourceMesh"/>.
		/// This is important when the graph has been saved to a file and is later loaded again, but the original mesh does not exist anymore (or has been moved).
		/// In that case we still need to be able to find the bounding box since the <see cref="CalculateTransform"/> method uses it.
		/// </summary>
		[JsonMember]
		Vector3 cachedSourceMeshBoundsMin;

		/// <summary>
		/// Radius to use when expanding navmesh cuts.
		///
		/// See: <see cref="NavmeshCut.radiusExpansionMode"/>
		/// </summary>
		[JsonMember]
		public float navmeshCuttingCharacterRadius = 0.5f;

		public override float NavmeshCuttingCharacterRadius => navmeshCuttingCharacterRadius;

		public override bool RecalculateNormals => recalculateNormals;

		public override float TileWorldSizeX => forcedBoundsSize.x;

		public override float TileWorldSizeZ => forcedBoundsSize.z;

		// Tiles are not supported, so this is irrelevant
		public override float MaxTileConnectionEdgeDistance => 0f;

		/// <summary>
		/// True if the point is inside the bounding box of this graph.
		///
		/// Warning: If your input mesh is entirely flat, the bounding box will also end up entirely flat (with a height of zero), this will make this function return false for almost all points, unless they are at exactly the right y-coordinate.
		///
		/// Note: For an unscanned graph, this will always return false.
		/// </summary>
		public override bool IsInsideBounds (Vector3 point)
        {
            return default;
        }

        /// <summary>
        /// World bounding box for the graph.
        ///
        /// This always contains the whole graph.
        ///
        /// Note: Since this is an axis-aligned bounding box, it may not be particularly tight if the graph is significantly rotated.
        ///
        /// If no mesh has been assigned, this will return a zero sized bounding box at the origin.
        ///
        /// [Open online documentation to see images]
        /// </summary>
        public override Bounds bounds
        {
            get
            {
                if (sourceMesh == null) return default;
                var m = (float4x4)CalculateTransform().matrix;
                var b = new ToWorldMatrix(new float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz)).ToWorld(new Bounds(Vector3.zero, sourceMesh.bounds.size * scale));
                return b;
            }
        }

        public override GraphTransform CalculateTransform()
        {
            return default;
        }

        class NavMeshGraphUpdatePromise : IGraphUpdatePromise
        {
            public NavMeshGraph graph;
			public List<GraphUpdateObject> graphUpdates;

			public void Apply (IGraphUpdateContext ctx) {
            }
        }

        IGraphUpdatePromise IUpdatableGraph.ScheduleGraphUpdates(List<GraphUpdateObject> graphUpdates) => new NavMeshGraphUpdatePromise { graph = this, graphUpdates = graphUpdates };

        public static void UpdateArea(GraphUpdateObject o, INavmeshHolder graph)
        {
        }

        class NavMeshGraphScanPromise : IGraphUpdatePromise {
			public NavMeshGraph graph;
			bool emptyGraph;
			GraphTransform transform;
			NavmeshTile[] tiles;
			Vector3 forcedBoundsSize;
			IntRect tileRect;
			NavmeshUpdates.NavmeshUpdateSettings cutSettings;

			public IEnumerator<JobHandle> Prepare () {
                return default;
            }

            public void Apply(IGraphUpdateContext ctx)
            {
            }
        }

        protected override IGraphUpdatePromise ScanInternal(bool async) => new NavMeshGraphScanPromise { graph = this };

        protected override void PostDeserialization(GraphSerializationContext ctx)
        {
        }
    }
}
