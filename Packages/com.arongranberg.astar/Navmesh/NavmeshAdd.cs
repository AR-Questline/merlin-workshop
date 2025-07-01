using UnityEngine;

namespace Pathfinding {
	using Pathfinding.Drawing;
	using Pathfinding.Pooling;
	using Pathfinding.Collections;

	/// <summary>
	/// Adds new geometry to a recast graph.
	///
	/// This component will add new geometry to a recast graph similar
	/// to how a NavmeshCut component removes it.
	///
	/// There are quite a few limitations to this component though.
	/// This navmesh geometry will not be connected to the rest of the navmesh
	/// in the same tile unless very exactly positioned so that the
	/// triangles line up exactly.
	/// It will be connected to neighbouring tiles if positioned so that
	/// it lines up with the tile border.
	///
	/// This component has a few very specific use-cases.
	/// For example if you have a tiled recast graph
	/// this component could be used to add bridges
	/// in that world.
	/// You would create a NavmeshCut object cutting out a hole for the bridge.
	/// then add a NavmeshAdd object which fills that space.
	/// Make sure NavmeshCut.CutsAddedGeom is disabled on the NavmeshCut, otherwise it will
	/// cut away the NavmeshAdd object.
	/// Then you can add links between the added geometry and the rest of the world, preferably using NodeLink3.
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/navmeshadd.html")]
	public class NavmeshAdd : NavmeshClipper {
		public enum MeshType {
			Rectangle,
			CustomMesh
		}

		public MeshType type;

		/// <summary>
		/// Custom mesh to use.
		/// The contour(s) of the mesh will be extracted.
		/// If you get the "max perturbations" error when cutting with this, check the normals on the mesh.
		/// They should all point in the same direction. Try flipping them if that does not help.
		/// </summary>
		public Mesh mesh;

		/// <summary>Cached vertices</summary>
		Vector3[] verts;

		/// <summary>Cached triangles</summary>
		int[] tris;

		/// <summary>Size of the rectangle</summary>
		public Vector2 rectangleSize = new Vector2(1, 1);

		public float meshScale = 1;

		public Vector3 center;

		/// <summary>cached transform component</summary>
		protected Transform tr;
		
		protected override void Awake () {
        }

        public Vector3 Center {
			get {
				return tr.position + (useRotationAndScale ? tr.TransformPoint(center) : center);
			}
		}

		/// <summary>
		/// Rebuild the internal mesh representation.
		///
		/// Use this if you have changed any settings during runtime.
		/// </summary>
		[ContextMenu("Rebuild Mesh")]
		public void RebuildMesh () {
        }

        /// <summary>
        /// Bounds in XZ space after transforming using the *inverse* transform of the inverseTransform parameter.
        /// The transformation will typically transform the vertices to graph space and this is used to
        /// figure out which tiles the add intersects.
        /// </summary>
        public override Rect GetBounds (Pathfinding.Util.GraphTransform inverseTransform, float radiusMargin) {
            return default;
        }

        /// <summary>Copy the mesh to the vertex and triangle buffers after the vertices have been transformed using the inverse of the inverseTransform parameter.</summary>
        /// <param name="vbuffer">Assumed to be either null or an array which has a length of zero or a power of two. If this mesh has more
        ///  vertices than can fit in the buffer then the buffer will be pooled using Pathfinding.Pooling.ArrayPool.Release and
        ///  a new sufficiently large buffer will be taken from the pool.</param>
        /// <param name="tbuffer">This will be set to the internal triangle buffer. You must not modify this array.</param>
        /// <param name="vertexCount">This will be set to the number of vertices in the vertex buffer.</param>
        /// <param name="inverseTransform">All vertices will be transformed using the #Pathfinding.GraphTransform.InverseTransform method.
        ///  This is typically used to transform from world space to graph space.</param>
        public void GetMesh(ref Int3[] vbuffer, out int[] tbuffer, out int vertexCount, Pathfinding.Util.GraphTransform inverseTransform = null)
        {
            tbuffer = default(int[]);
            vertexCount = default(int);
        }

        public static readonly Color GizmoColor = new Color(154.0f/255, 35.0f/255, 239.0f/255);

#if UNITY_EDITOR
		public static Int3[] gizmoBuffer;

		public override void DrawGizmos () {
        }
#endif
    }
}
