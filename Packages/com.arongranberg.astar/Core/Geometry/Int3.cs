using UnityEngine;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace Pathfinding {
	/// <summary>
	/// Holds a coordinate in (integer) millimeters.
	///
	/// This is used for node coordinates and other things, primarily to avoid floating point calculations in the core pathfinding routines (as they can be slow and non-deterministic if you are not careful).
	///
	/// You can cast back and forth between Vector3s and Int3s like:
	/// <code>
	/// Int3 intPoint = (Int3)transform.position;
	/// transform.position = (Vector3)intPoint;
	/// </code>
	///
	/// During the cast, the coordinates will be rounded to the nearest millimeter.
	/// </summary>
	public struct Int3 : System.IEquatable<Int3> {
		public int x;
		public int y;
		public int z;

		//These should be set to the same value (only PrecisionFactor should be 1 divided by Precision)

		/// <summary>
		/// Precision for the integer coordinates.
		/// One world unit is divided into [value] pieces. A value of 1000 would mean millimeter precision, a value of 1 would mean meter precision (assuming 1 world unit = 1 meter).
		/// This value affects the maximum coordinates for nodes as well as how large the cost values are for moving between two nodes.
		/// A higher value means that you also have to set all penalty values to a higher value to compensate since the normal cost of moving will be higher.
		/// </summary>
		public const int Precision = 1000;

		/// <summary><see cref="Precision"/> as a float</summary>
		public const float FloatPrecision = 1000F;

		/// <summary>1 divided by <see cref="Precision"/></summary>
		public const float PrecisionFactor = 0.001F;

		public static Int3 zero => new Int3();

		[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
		public Int3 (Vector3 position) : this()
        {
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
		public Int3 (int _x, int _y, int _z) : this()
        {
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static bool operator == (Int3 lhs, Int3 rhs) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static bool operator != (Int3 lhs, Int3 rhs) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static explicit operator Int3 (Vector3 ob) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static explicit operator Vector3 (Int3 ob) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static explicit operator float3 (Int3 ob) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static explicit operator int3 (Int3 ob) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static Int3 operator - (Int3 lhs, Int3 rhs) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static Int3 operator - (Int3 lhs) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static Int3 operator + (Int3 lhs, Int3 rhs) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public static Int3 operator * (Int3 lhs, int rhs) {
            return default;
        }

        public static Int3 operator * (Int3 lhs, float rhs) {
            return default;
        }

        public static Int3 operator * (Int3 lhs, double rhs) {
            return default;
        }

        public static Int3 operator / (Int3 lhs, float rhs) {
            return default;
        }

        public int this[int i] {
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
			get {
				return i == 0 ? x : (i == 1 ? y : z);
			}
			[MethodImplAttribute(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
			set {
				if (i == 0) x = value;
				else if (i == 1) y = value;
				else z = value;
			}
		}

		[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
		public static Int3 Max (Int3 lhs, Int3 rhs) {
            return default;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
		public static Int3 Min (Int3 lhs, Int3 rhs) {
            return default;
        }

        /// <summary>Angle between the vectors in radians</summary>
        public static float Angle (Int3 lhs, Int3 rhs) {
            return default;
        }

        public static int Dot (Int3 lhs, Int3 rhs) {
            return default;
        }

        public static long DotLong (Int3 lhs, Int3 rhs) {
            return default;
        }

        /// <summary>
        /// Normal in 2D space (XZ).
        /// Equivalent to Cross(this, Int3(0,1,0) )
        /// except that the Y coordinate is left unchanged with this operation.
        /// </summary>
        public Int3 Normal2D () {
            return default;
        }

        /// <summary>
        /// Returns the magnitude of the vector. The magnitude is the 'length' of the vector from 0,0,0 to this point. Can be used for distance calculations:
        /// <code> Debug.Log ("Distance between 3,4,5 and 6,7,8 is: "+(new Int3(3,4,5) - new Int3(6,7,8)).magnitude); </code>
        /// </summary>
        public float magnitude {
			get {
				//It turns out that using doubles is just as fast as using ints with Mathf.Sqrt. And this can also handle larger numbers (possibly with small errors when using huge numbers)!

				double _x = x;
				double _y = y;
				double _z = z;

				return (float)System.Math.Sqrt(_x*_x+_y*_y+_z*_z);
			}
		}

		/// <summary>
		/// Magnitude used for the cost between two nodes. The default cost between two nodes can be calculated like this:
		/// <code> int cost = (node1.position-node2.position).costMagnitude; </code>
		///
		/// This is simply the magnitude, rounded to the nearest integer
		/// </summary>
		public int costMagnitude {
			get {
				return (int)System.Math.Round(magnitude);
			}
		}

		/// <summary>The squared magnitude of the vector</summary>
		public float sqrMagnitude {
			get {
				double _x = x;
				double _y = y;
				double _z = z;
				return (float)(_x*_x+_y*_y+_z*_z);
			}
		}

		/// <summary>The squared magnitude of the vector</summary>
		public long sqrMagnitudeLong {
			get {
				long _x = x;
				long _y = y;
				long _z = z;
				return (_x*_x+_y*_y+_z*_z);
			}
		}

		public static implicit operator string (Int3 obj) {
            return default;
        }

        /// <summary>Returns a nicely formatted string representing the vector</summary>
        public override string ToString()
        {
            return default;
        }

        public override bool Equals(System.Object obj)
        {
            return default;
        }

        #region IEquatable implementation

        public bool Equals(Int3 other)
        {
            return default;
        }

        #endregion

        public override int GetHashCode()
        {
            return default;
        }
    }
}
