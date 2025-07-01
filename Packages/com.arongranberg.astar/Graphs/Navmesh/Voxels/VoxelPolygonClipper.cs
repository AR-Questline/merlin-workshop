using Pathfinding.Util;
using Unity.Burst;
using Pathfinding.Collections;

namespace Pathfinding.Graphs.Navmesh.Voxelization {
	/// <summary>Utility for clipping polygons</summary>
	internal struct Int3PolygonClipper {
		unsafe fixed float clipPolygonCache[7*3];

		/// <summary>
		/// Clips a polygon against an axis aligned half plane.
		///
		/// Returns: Number of output vertices
		///
		/// The vertices will be scaled and then offset, after that they will be cut using either the
		/// x axis, y axis or the z axis as the cutting line. The resulting vertices will be added to the
		/// vOut array in their original space (i.e before scaling and offsetting).
		/// </summary>
		/// <param name="vIn">Input vertices</param>
		/// <param name="n">Number of input vertices (may be less than the length of the vIn array)</param>
		/// <param name="vOut">Output vertices, needs to be large enough</param>
		/// <param name="multi">Scale factor for the input vertices</param>
		/// <param name="offset">Offset to move the input vertices with before cutting</param>
		/// <param name="axis">Axis to cut along, either x=0, y=1, z=2</param>
		public int ClipPolygon (UnsafeSpan<Int3> vIn, int n, UnsafeSpan<Int3> vOut, int multi, int offset, int axis) {
            return default;
        }
    }

	/// <summary>Utility for clipping polygons</summary>
	internal struct VoxelPolygonClipper {
		public unsafe fixed float x[8];
		public unsafe fixed float y[8];
		public unsafe fixed float z[8];
		public int n;

		public UnityEngine.Vector3 this[int i] {
			set {
				unsafe {
					x[i] = value.x;
					y[i] = value.y;
					z[i] = value.z;
				}
			}
		}

		/// <summary>
		/// Clips a polygon against an axis aligned half plane.
		/// The polygons stored in this object are clipped against the half plane at x = -offset.
		/// </summary>
		/// <param name="result">Ouput vertices</param>
		/// <param name="multi">Scale factor for the input vertices. Should be +1 or -1. If -1 the negative half plane is kept.</param>
		/// <param name="offset">Offset to move the input vertices with before cutting</param>
		public void ClipPolygonAlongX ([NoAlias] ref VoxelPolygonClipper result, float multi, float offset) {
        }

        /// <summary>
        /// Clips a polygon against an axis aligned half plane.
        /// The polygons stored in this object are clipped against the half plane at z = -offset.
        /// </summary>
        /// <param name="result">Ouput vertices. Only the Y and Z coordinates are calculated. The X coordinates are undefined.</param>
        /// <param name="multi">Scale factor for the input vertices. Should be +1 or -1. If -1 the negative half plane is kept.</param>
        /// <param name="offset">Offset to move the input vertices with before cutting</param>
        public void ClipPolygonAlongZWithYZ ([NoAlias] ref VoxelPolygonClipper result, float multi, float offset) {
        }

        /// <summary>
        /// Clips a polygon against an axis aligned half plane.
        /// The polygons stored in this object are clipped against the half plane at z = -offset.
        /// </summary>
        /// <param name="result">Ouput vertices. Only the Y coordinates are calculated. The X and Z coordinates are undefined.</param>
        /// <param name="multi">Scale factor for the input vertices. Should be +1 or -1. If -1 the negative half plane is kept.</param>
        /// <param name="offset">Offset to move the input vertices with before cutting</param>
        public void ClipPolygonAlongZWithY ([NoAlias] ref VoxelPolygonClipper result, float multi, float offset) {
        }
    }
}
