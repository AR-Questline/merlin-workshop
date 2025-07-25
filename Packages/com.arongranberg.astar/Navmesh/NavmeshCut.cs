
using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Pooling;
	using Pathfinding.Drawing;
	using Pathfinding.Util;
	using Pathfinding.Collections;
	using Unity.Burst;
	using Unity.Collections;
	using Unity.Mathematics;
	using Unity.Collections.LowLevel.Unsafe;

	/// <summary>
	/// Navmesh cutting is used for fast recast/navmesh graph updates.
	///
	/// Navmesh cutting is used to cut holes in an existing navmesh generated by a recast or navmesh graph.
	/// With navmesh cutting you can remove (cut) parts of the navmesh that is blocked by obstacles such as a new building in an RTS game however you cannot add anything new to the navmesh or change
	/// the positions of the nodes.
	///
	/// Normal graph updates on recast/navmesh graphs, in contrast, only allow either just changing parameters on existing nodes (e.g make a whole triangle unwalkable), which is not very flexible, or recalculate whole tiles, which can be slow.
	/// Navmesh cutting is typically significantly faster than recalculating whole tiles from scratch in a recast graph.
	///
	/// [Open online documentation to see videos]
	///
	/// The NavmeshCut component uses a 2D shape to cut the navmesh with. This shape can be produced by either one of the built-in 2D shapes (rectangle/circle) or one of the 3D shapes (cube/sphere/capsule)
	/// which will be projected down to a 2D shape when cutting happens. You can also specify a custom 2D mesh to use as a cut.
	///
	/// [Open online documentation to see images]
	///
	/// Note that the rectangle/circle shapes are not 3D. If you rotate them, you will see that the 2D shape will be rotated and then just projected down on the XZ plane.
	/// Therefore it is recommended to use the 3D shapes (cube/sphere/capsule) in most cases since those are easier to use.
	///
	/// In the scene view the NavmeshCut looks like an extruded 2D shape because a navmesh cut also has a height. It will only cut the part of the
	/// navmesh which it touches. For performance reasons it only checks the bounding boxes of the triangles in the navmesh, so it may cut triangles
	/// whoose bounding boxes it intersects even if the triangle does not intersect the extruded shape. However in most cases this does not make a large difference.
	///
	/// It is also possible to set the navmesh cut to dual mode by setting the <see cref="isDual"/> field to true. This will prevent it from cutting a hole in the navmesh
	/// and it will instead just split the navmesh along the border but keep both the interior and the exterior. This can be useful if you for example
	/// want to change the penalty of some region which does not neatly line up with the navmesh triangles. It is often combined with the GraphUpdateScene component
	/// (however note that the GraphUpdateScene component will not automatically reapply the penalty if the graph is updated again).
	///
	/// By default the navmesh cut does not take rotation or scaling into account. If you want to do that, you can set the <see cref="useRotationAndScale"/> field to true.
	///
	/// <b>Custom meshes</b>
	/// For most purposes you can use the built-in shapes, however in some cases a custom cutting mesh may be useful.
	/// The custom mesh should be a flat 2D shape like in the image below. The script will then find the contour of that mesh and use that shape as the cut.
	/// Make sure that all normals are smooth and that the mesh contains no UV information. Otherwise Unity might split a vertex and then the script will not
	/// find the correct contour. You should not use a very high polygon mesh since that will create a lot of nodes in the navmesh graph and slow
	/// down pathfinding because of that. For very high polygon meshes it might even cause more suboptimal paths to be generated if it causes many
	/// thin triangles to be added to the navmesh.
	/// [Open online documentation to see images]
	///
	/// <b>Update frequency</b>
	///
	/// Navmesh cuts are typically pretty fast, so you may be tempted to make them update the navmesh very often (once every few frames perhaps), just because you can spare the CPU power.
	/// However, updating the navmesh too often can also have consequences for agents that are following paths on the graph.
	///
	/// If a navmesh cut updates the graph near an agent, it will usually have to recalculate its path. If this happens too often, this can lead to the pathfinding
	/// worker threads being overwhelmed by pathfinding requests, causing higher latency for individual pathfinding requests. This can, in turn, make agents less responsive.
	///
	/// So it's recommended to keep the update frequency reasonable. After all, a player is unlikely to notice if the navmesh was updated 20 times per second or 2 times per second (or even less often).
	///
	/// You can primarily control this using the <see cref="updateDistance"/> and <see cref="updateRotationDistance"/> fields. But you can also control the global frequency of updates. This is explained in the next section.
	///
	/// <b>Control updates through code</b>
	/// Navmesh cuts are applied periodically, but sometimes you may want to ensure the graph is up to date right now.
	/// Then you can use the following code.
	/// <code>
	/// // Schedule pending updates to be done as soon as the pathfinding threads
	/// // are done with what they are currently doing.
	/// AstarPath.active.navmeshUpdates.ForceUpdate();
	/// // Block until the updates have finished
	/// AstarPath.active.FlushGraphUpdates();
	/// </code>
	///
	/// You can also control how often the scripts check for if any navmesh cut has changed.
	/// If you have a very large number of cuts it may be good for performance to not check it as often.
	/// <code>
	/// // Check every frame (the default)
	/// AstarPath.active.navmeshUpdates.updateInterval = 0;
	///
	/// // Check every 0.1 seconds
	/// AstarPath.active.navmeshUpdates.updateInterval = 0.1f;
	///
	/// // Never check for changes
	/// AstarPath.active.navmeshUpdates.updateInterval = -1;
	/// // You will have to schedule updates manually using
	/// AstarPath.active.navmeshUpdates.ForceUpdate();
	/// </code>
	///
	/// You can also find this setting in the AstarPath inspector under Settings.
	/// [Open online documentation to see images]
	///
	/// <b>Navmesh cutting and tags/penalties</b>
	/// Navmesh cuts can only preserve tags for updates which happen when the graph is first scanned, or when a recast graph tile is recalculated from scratch.
	///
	/// This means that any tags that you apply dynamically using e.g. a <see cref="GraphUpdateScene"/> component may be lost when a navmesh cut is applied.
	/// If you need to combine tags and navmesh cutting, it is therefore strongly recommended to use the <see cref="RecastMeshObjStatic"/> component to apply the tags,
	/// as that will work smoothly with navmesh cutting.
	///
	/// Internally, what happens is that when a graph is scanned, the navmesh cutting subsystem takes a snapshot of all triangles in the graph, including tags. This data will then be referenced
	/// every time cutting happens and the tags from the snapshot will be copied to the new triangles after cutting has taken place.
	///
	/// You can also apply tags and penalties using a graph update after cutting has taken place. For example by subclassing a navmesh cut and overriding the <see cref="UsedForCut"/> method.
	/// However, it is recommended to use the <see cref="RecastMeshObjStatic"/> as mentioned before, as this is a more robust solution.
	///
	/// See: http://www.arongranberg.com/2013/08/navmesh-cutting/
	///
	/// Version: This component is only available in 2022.3 and later, due to Unity bugs in earlier versions.
	/// </summary>
	[AddComponentMenu("Pathfinding/Navmesh/Navmesh Cut")]
	[ExecuteAlways]
	[HelpURL("https://arongranberg.com/astar/documentation/stable/navmeshcut.html")]
	public class NavmeshCut : NavmeshClipper {
		public enum MeshType {
			/// <summary>A 2D rectangle</summary>
			Rectangle,
			/// <summary>A 2D circle</summary>
			Circle,
			CustomMesh,
			/// <summary>A 3D box which will be projected down to a 2D outline</summary>
			Box,
			/// <summary>A 3D sphere which will be projected down to a 2D outline</summary>
			Sphere,
			/// <summary>A 3D capsule which will be projected down to a 2D outline</summary>
			Capsule,
		}

		public enum RadiusExpansionMode {
			/// <summary>
			/// If DontExpand is used then the cut will be exactly as specified with no modifications.
			/// It will be the same for all graphs.
			/// </summary>
			DontExpand,
			/// <summary>
			/// If ExpandByAgentRadius is used then the cut will be expanded by the agent's radius (set in the recast graph settings)
			/// in every direction. For navmesh graphs (which do not have a character radius) this is equivalent to DontExpand.
			///
			/// This is especially useful if you have multiple graphs for different unit sizes and want the cuts to be sized according to
			/// the different units.
			/// </summary>
			ExpandByAgentRadius,
		}

		/// <summary>Shape of the cut</summary>
		[Tooltip("Shape of the cut")]
		public MeshType type = MeshType.Box;

		/// <summary>
		/// Custom mesh to use.
		/// The contour(s) of the mesh will be extracted.
		/// If you get the "max perturbations" error when cutting with this, check the normals on the mesh.
		/// They should all point in the same direction. Try flipping them if that does not help.
		///
		/// This mesh should only be a 2D surface, not a volume.
		/// </summary>
		[Tooltip("The contour(s) of the mesh will be extracted. This mesh should only be a 2D surface, not a volume (see documentation).")]
		public Mesh mesh;

		/// <summary>Size of the rectangle</summary>
		public Vector2 rectangleSize = new Vector2(1, 1);

		/// <summary>Radius of the circle</summary>
		public float circleRadius = 1;

		/// <summary>Number of vertices on the circle</summary>
		public int circleResolution = 6;

		/// <summary>The cut will be extruded to this height</summary>
		public float height = 1;

		/// <summary>Scale of the custom mesh, if used</summary>
		[Tooltip("Scale of the custom mesh")]
		public float meshScale = 1;

		public Vector3 center;
		
		/// <summary>
		/// Only makes a split in the navmesh, but does not remove the geometry to make a hole.
		/// This is slower than a normal cut
		/// </summary>
		[Tooltip("Only makes a split in the navmesh, but does not remove the geometry to make a hole")]
		public bool isDual;

		/// <summary>
		/// If the cut should be expanded by the agent radius or not.
		///
		/// See <see cref="RadiusExpansionMode"/> for more details.
		/// </summary>

		public RadiusExpansionMode radiusExpansionMode = RadiusExpansionMode.ExpandByAgentRadius;

		/// <summary>
		/// Cuts geometry added by a NavmeshAdd component.
		/// You rarely need to change this
		/// </summary>
		public bool cutsAddedGeom = true;

		NativeList<float3> meshContourVertices;
		NativeList<ContourBurst> meshContours;

		/// <summary>cached transform component</summary>
		protected Transform tr;
		Mesh lastMesh;

		protected override void Awake () {
        }

        protected override void OnDisable ()
        {
        }

        /// <summary>Cached variable, to avoid allocations</summary>
        static readonly Dictionary<Vector2Int, int> edges = new Dictionary<Vector2Int, int>();
        /// <summary>Cached variable, to avoid allocations</summary>
        static readonly Dictionary<int, int> pointers = new Dictionary<int, int>();

        /// <summary>
        /// Called whenever this navmesh cut is used to update the navmesh.
        /// Called once for each tile the navmesh cut is in.
        /// You can override this method to execute custom actions whenever this happens.
        /// </summary>
        public virtual void UsedForCut()
        {
        }

        void CalculateMeshContour()
        {
        }

        /// <summary>
        /// Bounds in XZ space after transforming using the *inverse* transform of the inverseTransform parameter.
        /// The transformation will typically transform the vertices to graph space and this is used to
        /// figure out which tiles the cut intersects.
        /// </summary>
        public override Rect GetBounds(GraphTransform inverseTransform, float radiusMargin)
        {
            return default;
        }

        public struct Contour
        {
            public float ymin;
            public float ymax;
            public List<Vector2> contour;
		}

        public struct ContourBurst {
			public int startIndex;
			public int endIndex;
			public float ymin;
			public float ymax;
		}

		Matrix4x4 contourTransformationMatrix {
			get {
				// Take rotation and scaling into account
				if (useRotationAndScale) {
					return tr.localToWorldMatrix * Matrix4x4.Translate(center + Vector3.down * AstarPath.NavGraphsOffset);
				} else {
					return Matrix4x4.Translate(tr.position + center + Vector3.down * AstarPath.NavGraphsOffset);
				}
			}
		}

		/// <summary>
		/// Contour of the navmesh cut.
		/// Fills the specified buffer with all contours.
		/// The cut may contain several contours which is why the buffer is a list of lists.
		/// </summary>
		/// <param name="buffer">Will be filled with the result</param>
		/// <param name="matrix">All points will be transformed using this matrix. They are in world space before the transformation. Typically this a transform that maps from world space to graph space.</param>
		/// <param name="radiusMargin">The obstacle will be expanded by this amount. Typically this is the character radius for the graph. The MeshType.CustomMesh does not support this.
		/// If #radiusExpansionMode is RadiusExpansionMode.DontExpand then this parameter is ignored.</param>
		public void GetContour (List<Contour> buffer, Matrix4x4 matrix, float radiusMargin) {
        }

        /// <summary>
        /// Contour of the navmesh cut.
        /// Fills the specified buffer with all contours.
        /// The cut may contain several contours.
        /// </summary>
        /// <param name="outputVertices">Will be filled with all vertices</param>
        /// <param name="outputContours">Will be filled with all contours that reference the outputVertices list.</param>
        /// <param name="matrix">All points will be transformed using this matrix. They are in world space before the transformation. Typically this a matrix that maps from world space to graph space.</param>
        /// <param name="radiusMargin">The obstacle will be expanded by this amount. Typically this is the character radius for the graph. The MeshType.CustomMesh does not support this.</param>
        public unsafe void GetContourBurst(UnsafeList<float2>* outputVertices, UnsafeList<ContourBurst>* outputContours, Matrix4x4 matrix, float radiusMargin)
        {
        }

        public static readonly Color GizmoColor = new Color(37.0f / 255, 184.0f / 255, 239.0f / 255);
        public static readonly Color GizmoColor2 = new Color(169.0f/255, 92.0f/255, 242.0f/255);

		public override void DrawGizmos ()
        {
        }

        protected override void OnUpgradeSerializedData(ref Serialization.Migrations migrations, bool unityThread)
        {
        }
    }

    [BurstCompile]
    internal static class NavmeshCutJobs
    {
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
        public static unsafe void CalculateContour(ref JobCalculateContour job)
        {
        }

        static readonly float4[] BoxCorners = new float4[] {
            new float4(-0.5f, -0.5f, -0.5f, 1.0f),
            new float4(+0.5f, -0.5f, -0.5f, 1.0f),
            new float4(-0.5f, +0.5f, -0.5f, 1.0f),
            new float4(+0.5f, +0.5f, -0.5f, 1.0f),
            new float4(-0.5f, -0.5f, +0.5f, 1.0f),
            new float4(+0.5f, -0.5f, +0.5f, 1.0f),
            new float4(-0.5f, +0.5f, +0.5f, 1.0f),
            new float4(+0.5f, +0.5f, +0.5f, 1.0f),
        };

        public struct JobCalculateContour
        {
            public unsafe Unity.Collections.LowLevel.Unsafe.UnsafeList<float2>* outputVertices;
            public unsafe Unity.Collections.LowLevel.Unsafe.UnsafeList<NavmeshCut.ContourBurst>* outputContours;
            public unsafe Unity.Collections.LowLevel.Unsafe.UnsafeList<NavmeshCut.ContourBurst>* meshContours;
            public unsafe Unity.Collections.LowLevel.Unsafe.UnsafeList<float3>* meshContourVertices;
            public float4x4 matrix;
            public float4x4 localToWorldMatrix;
            public float radiusMargin;
            public int circleResolution;
            public float circleRadius;
            public float2 rectangleSize;
            public float height;
            public float meshScale;
            public NavmeshCut.MeshType meshType;

            public unsafe void Execute()
            {
            }

            /// <summary>Winds the vertices correctly. The particular winding doesn't matter, but all cuts must have the same winding order.</summary>
            private unsafe void WindCounterClockwise(UnsafeList<float2>* vertices, int startIndex, int endIndex)
            {
            }
        }

        /// <summary>
        /// Adjust the radius so that the contour better approximates a circle.
        /// Instead of all points laying exactly on the circle, which means all of the contour is inside the circle,
        /// we change it so that half of the contour is inside and half is outside.
        ///
        /// Returns the new radius
        /// </summary>
        static float ApproximateCircleWithPolylineRadius(float radius, int resolution)
        {
            return default;
        }

        public static unsafe void CapsuleConvexHullXZ(float4x4 matrix, UnsafeList<float2>* points, float height, float radius, float radiusMargin, int circleResolution, out int numPoints, out float minY, out float maxY)
        {
            numPoints = default(int);
            minY = default(float);
            maxY = default(float);
        }

        public static unsafe void BoxConvexHullXZ(float4x4 matrix, UnsafeList<float2>* points, out int numPoints, out float minY, out float maxY)
        {
            numPoints = default(int);
            minY = default(float);
            maxY = default(float);
        }

        struct AngleComparator : IComparer<float2>
        {
            public float2 origin;
            public int Compare(float2 lhs, float2 rhs)
            {
                return default;
            }
        }

        /// <summary>
        /// Calculates the convex hull of a point set using the graham scan algorithm.
        ///
        /// The `points` array will be modified to contain the convex hull.
        /// The number of vertices on the hull is returned by this function.
        ///
        /// Vertices on the hull closer than `vertexMergeDistance` will be merged together.
        ///
        /// From KTH ACM Contest Template Library (2015 version)
        /// </summary>
        public static unsafe int ConvexHull(float2* points, int nPoints, float vertexMergeDistance)
        {
            return default;
        }
    }
}
