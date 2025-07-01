using Unity.Mathematics;
using UnityEngine;
using Pathfinding.Collections;
using Pathfinding.Pooling;

namespace Pathfinding.Util {
	/// <summary>
	/// Transforms to and from world space to a 2D movement plane.
	/// The transformation is guaranteed to be purely a rotation
	/// so no scale or offset is used. This interface is primarily
	/// used to make it easier to write movement scripts which can
	/// handle movement both in the XZ plane and in the XY plane.
	///
	/// See: <see cref="Pathfinding.Util.GraphTransform"/>
	/// </summary>
	public interface IMovementPlane {
		Vector2 ToPlane(Vector3 p);
		Vector2 ToPlane(Vector3 p, out float elevation);
		Vector3 ToWorld(Vector2 p, float elevation = 0);
		SimpleMovementPlane ToSimpleMovementPlane();
	}

	/// <summary>
	/// A matrix wrapper which can be used to project points from world space to a movement plane.
	///
	/// In contrast to <see cref="NativeMovementPlane"/>, this is represented by a matrix instead of a quaternion.
	/// This means it is less space efficient (36 bytes instead of 16 bytes) but it is more performant when
	/// you need to do a lot of ToPlane conversions.
	/// </summary>
	public readonly struct ToPlaneMatrix {
		public readonly float3x3 matrix;

		public ToPlaneMatrix (NativeMovementPlane plane) => this.matrix = new float3x3(math.conjugate(plane.rotation));

		/// <summary>
		/// Transforms from world space to the 'ground' plane of the graph.
		/// The transformation is purely a rotation so no scale or offset is used.
		///
		/// See: <see cref="NativeMovementPlane.ToPlane(float3)"/>
		/// </summary>
		public float2 ToPlane(float3 p) => math.mul(matrix, p).xz;

		/// <summary>
		/// Transforms from world space to the 'ground' plane of the graph.
		/// The transformation is purely a rotation so no scale or offset is used.
		///
		/// The elevation coordinate will be returned as the y coordinate of the returned vector.
		///
		/// See: <see cref="NativeMovementPlane.ToPlane(float3)"/>
		/// </summary>
		public float3 ToXZPlane(float3 p) => math.mul(matrix, p);

		/// <summary>
		/// Transforms from world space to the 'ground' plane of the graph.
		/// The transformation is purely a rotation so no scale or offset is used.
		///
		/// See: <see cref="NativeMovementPlane.ToPlane(float3)"/>
		/// </summary>
		public float2 ToPlane (float3 p, out float elevation) {
            elevation = default(float);
            return default;
        }
    }

	/// <summary>
	/// A matrix wrapper which can be used to project points from a movement plane to world space.
	///
	/// In contrast to <see cref="NativeMovementPlane"/>, this is represented by a matrix instead of a quaternion.
	/// This means it is less space efficient (36 bytes instead of 16 bytes) but it is more performant when
	/// you need to do a lot of ToWorld conversions.
	/// </summary>
	public readonly struct ToWorldMatrix {
		public readonly float3x3 matrix;

		public ToWorldMatrix (NativeMovementPlane plane) => this.matrix = new float3x3(plane.rotation);

		public ToWorldMatrix (float3x3 matrix) => this.matrix = matrix;

		public float3 ToWorld(float2 p, float elevation = 0) => math.mul(matrix, new float3(p.x, elevation, p.y));

		/// <summary>
		/// Transforms a bounding box from local space to world space.
		///
		/// The Y coordinate of the bounding box is the elevation coordinate.
		///
		/// See: https://zeux.io/2010/10/17/aabb-from-obb-with-component-wise-abs/
		/// </summary>
		public Bounds ToWorld (Bounds bounds) {
            return default;
        }
    }

	/// <summary>A variant of <see cref="SimpleMovementPlane"/> that can be passed to burst functions</summary>
	public readonly struct NativeMovementPlane {
		/// <summary>
		/// The rotation of the plane.
		/// The plane is defined by the XZ-plane rotated by this quaternion.
		///
		/// Should always be normalized.
		/// </summary>
		public readonly quaternion rotation;

		/// <summary>Normal of the plane</summary>
		// TODO: Check constructor for float3x3(quaternion), seems smarter, at least in burst
		public float3 up => 2 * new float3(rotation.value.x * rotation.value.y - rotation.value.w * rotation.value.z, 0.5f - rotation.value.x * rotation.value.x - rotation.value.z * rotation.value.z, rotation.value.w * rotation.value.x + rotation.value.y * rotation.value.z); // math.mul(rotation, Vector3.up);

		public NativeMovementPlane(quaternion rotation) : this()
        {
        }

        public NativeMovementPlane(SimpleMovementPlane plane) : this(plane.rotation) {}

        public ToPlaneMatrix AsWorldToPlaneMatrix() => new ToPlaneMatrix(this);
		public ToWorldMatrix AsPlaneToWorldMatrix() => new ToWorldMatrix(this);

		/// <summary>A movement plane that has the given up direction, but is otherwise as similar as possible to this movement plane</summary>
		public NativeMovementPlane MatchUpDirection (float3 up) {
            return default;
        }

        public float ProjectedLength(float3 v) => math.length(ToPlane(v));

		/// <summary>
		/// Transforms from world space to the 'ground' plane of the graph.
		/// The transformation is purely a rotation so no scale or offset is used.
		///
		/// For a graph rotated with the rotation (-90, 0, 0) this will transform
		/// a coordinate (x,y,z) to (x,y). For a graph with the rotation (0,0,0)
		/// this will tranform a coordinate (x,y,z) to (x,z). More generally for
		/// a graph with a quaternion rotation R this will transform a vector V
		/// to inverse(R) * V (i.e rotate the vector V using the inverse of rotation R).
		/// </summary>
		public float2 ToPlane (float3 p) {
            return default;
        }

        /// <summary>Transforms from world space to the 'ground' plane of the graph</summary>
        public float2 ToPlane(float3 p, out float elevation)
        {
            elevation = default(float);
            return default;
        }

        /// <summary>
        /// Transforms from the 'ground' plane of the graph to world space.
        /// The transformation is purely a rotation so no scale or offset is used.
        /// </summary>
        public float3 ToWorld(float2 p, float elevation = 0f)
        {
            return default;
        }

        /// <summary>
        /// Projects a rotation onto the plane.
        ///
        /// The returned angle is such that
        ///
        /// <code>
        /// var angle = ...;
        /// var q = math.mul(plane.rotation, quaternion.RotateY(angle));
        /// AstarMath.DeltaAngle(plane.ToPlane(q), -angle) == 0; // or at least approximately equal
        /// </code>
        ///
        /// See: <see cref="ToWorldRotation"/>
        /// See: <see cref="ToWorldRotationDelta"/>
        /// </summary>
        /// <param name="rotation">the rotation to project</param>
        public float ToPlane(quaternion rotation)
        {
            return default;
        }

        public quaternion ToWorldRotation(float angle)
        {
            return default;
        }

        public quaternion ToWorldRotationDelta(float deltaAngle)
        {
            return default;
        }

        /// <summary>
        /// Transforms a bounding box from local space to world space.
        ///
        /// The Y coordinate of the bounding box is the elevation coordinate.
        /// </summary>
        public Bounds ToWorld(Bounds bounds) => AsPlaneToWorldMatrix().ToWorld(bounds);
	}

	/// <summary>
	/// Represents the orientation of a plane.
	///
	/// When a character walks around in the world, it may not necessarily walk on the XZ-plane.
	/// It may be the case that the character is on a spherical world, or maybe it walks on a wall or upside down on the ceiling.
	///
	/// A movement plane is used to handle this. It contains functions for converting a 3D point into a 2D point on that plane, and functions for converting back to 3D.
	///
	/// See: NativeMovementPlane
	/// </summary>
#if MODULE_COLLECTIONS_2_0_0_OR_NEWER && UNITY_2022_2_OR_NEWER
	[Unity.Collections.GenerateTestsForBurstCompatibility]
#endif
	public readonly struct SimpleMovementPlane : IMovementPlane {
		public readonly Quaternion rotation;
		public readonly Quaternion inverseRotation;
		readonly byte plane;
		public bool isXY => plane == 1;
		public bool isXZ => plane == 2;

		/// <summary>A plane that spans the X and Y axes</summary>
		public static readonly SimpleMovementPlane XYPlane = new SimpleMovementPlane(Quaternion.Euler(-90, 0, 0));

		/// <summary>A plane that spans the X and Z axes</summary>
		public static readonly SimpleMovementPlane XZPlane = new SimpleMovementPlane(Quaternion.identity);

		public SimpleMovementPlane (Quaternion rotation) : this()
        {
        }

        /// <summary>
        /// Transforms from world space to the 'ground' plane of the graph.
        /// The transformation is purely a rotation so no scale or offset is used.
        ///
        /// For a graph rotated with the rotation (-90, 0, 0) this will transform
        /// a coordinate (x,y,z) to (x,y). For a graph with the rotation (0,0,0)
        /// this will tranform a coordinate (x,y,z) to (x,z). More generally for
        /// a graph with a quaternion rotation R this will transform a vector V
        /// to inverse(R) * V (i.e rotate the vector V using the inverse of rotation R).
        /// </summary>
        public Vector2 ToPlane (Vector3 point) {
            return default;
        }

        /// <summary>
        /// Transforms from world space to the 'ground' plane of the graph.
        /// The transformation is purely a rotation so no scale or offset is used.
        ///
        /// For a graph rotated with the rotation (-90, 0, 0) this will transform
        /// a coordinate (x,y,z) to (x,y). For a graph with the rotation (0,0,0)
        /// this will tranform a coordinate (x,y,z) to (x,z). More generally for
        /// a graph with a quaternion rotation R this will transform a vector V
        /// to inverse(R) * V (i.e rotate the vector V using the inverse of rotation R).
        /// </summary>
        public float2 ToPlane (float3 point) {
            return default;
        }

        /// <summary>
        /// Transforms from world space to the 'ground' plane of the graph.
        /// The transformation is purely a rotation so no scale or offset is used.
        /// </summary>
        public Vector2 ToPlane (Vector3 point, out float elevation) {
            elevation = default(float);
            return default;
        }

        /// <summary>
        /// Transforms from world space to the 'ground' plane of the graph.
        /// The transformation is purely a rotation so no scale or offset is used.
        /// </summary>
        public float2 ToPlane (float3 point, out float elevation) {
            elevation = default(float);
            return default;
        }

        /// <summary>
        /// Transforms from the 'ground' plane of the graph to world space.
        /// The transformation is purely a rotation so no scale or offset is used.
        /// </summary>
        public Vector3 ToWorld (Vector2 point, float elevation = 0) {
            return default;
        }

        /// <summary>
        /// Transforms from the 'ground' plane of the graph to world space.
        /// The transformation is purely a rotation so no scale or offset is used.
        /// </summary>
        public float3 ToWorld(float2 point, float elevation = 0)
        {
            return default;
        }

        public SimpleMovementPlane ToSimpleMovementPlane()
        {
            return default;
        }

        public static bool operator ==(SimpleMovementPlane lhs, SimpleMovementPlane rhs)
        {
            return default;
        }

        public static bool operator !=(SimpleMovementPlane lhs, SimpleMovementPlane rhs)
        {
            return default;
        }

        public override bool Equals(System.Object other)
        {
            return default;
        }

        public override int GetHashCode()
        {
            return default;
        }
    }

	/// <summary>Generic 3D coordinate transformation</summary>
	public interface ITransform {
		Vector3 Transform(Vector3 position);
		Vector3 InverseTransform(Vector3 position);
	}

	/// <summary>Like <see cref="Pathfinding.Util.GraphTransform"/>, but mutable</summary>
	public class MutableGraphTransform : GraphTransform {
		public MutableGraphTransform (Matrix4x4 matrix) : base(matrix) {}

        /// <summary>Replace this transform with the given matrix transformation</summary>
        public void SetMatrix(Matrix4x4 matrix)
        {
        }
    }

	/// <summary>
	/// Defines a transformation from graph space to world space.
	/// This is essentially just a simple wrapper around a matrix, but it has several utilities that are useful.
	/// </summary>
	public class GraphTransform : IMovementPlane, ITransform {
		/// <summary>True if this transform is the identity transform (i.e it does not do anything)</summary>
		public bool identity { get { return isIdentity; } }

		/// <summary>True if this transform is a pure translation without any scaling or rotation</summary>
		public bool onlyTranslational { get { return isOnlyTranslational; } }

		bool isXY;
		bool isXZ;
		bool isOnlyTranslational;
		bool isIdentity;

		public Matrix4x4 matrix { get; private set; }
		public Matrix4x4 inverseMatrix { get; private set; }
		Vector3 up;
		Vector3 translation;
		Int3 i3translation;
		public Quaternion rotation { get; private set; }
		Quaternion inverseRotation;

		public static readonly GraphTransform identityTransform = new GraphTransform(Matrix4x4.identity);

		/// <summary>Transforms from the XZ plane to the XY plane</summary>
		public static readonly GraphTransform xyPlane = new GraphTransform(Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90, 0, 0), Vector3.one));

		/// <summary>Transforms from the XZ plane to the XZ plane (i.e. an identity transformation)</summary>
		public static readonly GraphTransform xzPlane = new GraphTransform(Matrix4x4.identity);

		public GraphTransform (Matrix4x4 matrix) {
        }

        protected void Set (Matrix4x4 matrix) {
        }

        public Vector3 WorldUpAtGraphPosition(Vector3 point)
        {
            return default;
        }

        static bool MatrixIsTranslational(Matrix4x4 matrix)
        {
            return default;
        }

        public Vector3 Transform(Vector3 point)
        {
            return default;
        }

        public Vector3 TransformVector(Vector3 dir)
        {
            return default;
        }

        public void Transform(Int3[] arr)
        {
        }

        public void Transform(UnsafeSpan<Int3> arr)
        {
        }

        public void Transform(Vector3[] arr)
        {
        }

        public Vector3 InverseTransform (Vector3 point)
        {
            return default;
        }

        public Vector3 InverseTransformVector(Vector3 dir)
        {
            return default;
        }

        public Int3 InverseTransform(Int3 point)
        {
            return default;
        }

        public void InverseTransform(Int3[] arr)
        {
        }

        public void InverseTransform(UnsafeSpan<Int3> arr)
        {
        }

        public static GraphTransform operator *(GraphTransform lhs, Matrix4x4 rhs)
        {
            return default;
        }

        public static GraphTransform operator *(Matrix4x4 lhs, GraphTransform rhs)
        {
            return default;
        }

        public Bounds Transform(Bounds bounds)
        {
            return default;
        }

        public Bounds InverseTransform(Bounds bounds)
        {
            return default;
        }

        #region IMovementPlane implementation

        /// <summary>
        /// Transforms from world space to the 'ground' plane of the graph.
        /// The transformation is purely a rotation so no scale or offset is used.
        ///
        /// For a graph rotated with the rotation (-90, 0, 0) this will transform
        /// a coordinate (x,y,z) to (x,y). For a graph with the rotation (0,0,0)
        /// this will tranform a coordinate (x,y,z) to (x,z). More generally for
        /// a graph with a quaternion rotation R this will transform a vector V
        /// to R * V (i.e rotate the vector V using the rotation R).
        /// </summary>
        Vector2 IMovementPlane.ToPlane(Vector3 point)
        {
            return default;
        }

        /// <summary>
        /// Transforms from world space to the 'ground' plane of the graph.
        /// The transformation is purely a rotation so no scale or offset is used.
        /// </summary>
        Vector2 IMovementPlane.ToPlane(Vector3 point, out float elevation)
        {
            elevation = default(float);
            return default;
        }

        /// <summary>
        /// Transforms from the 'ground' plane of the graph to world space.
        /// The transformation is purely a rotation so no scale or offset is used.
        /// </summary>
        Vector3 IMovementPlane.ToWorld (Vector2 point, float elevation) {
            return default;
        }

        public SimpleMovementPlane ToSimpleMovementPlane () {
            return default;
        }

        #endregion

        /// <summary>Copies the data in this transform to another mutable graph transform</summary>
        public void CopyTo (MutableGraphTransform graphTransform) {
        }
    }
}
