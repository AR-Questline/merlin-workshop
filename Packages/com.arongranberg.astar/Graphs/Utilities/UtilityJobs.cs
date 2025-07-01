namespace Pathfinding.Jobs {
	using UnityEngine;
	using Unity.Burst;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Mathematics;
	using UnityEngine.Assertions;
	using Pathfinding.Graphs.Grid;
	using Pathfinding.Collections;

	/// <summary>
	/// Slice of a 3D array.
	///
	/// This is a helper struct used in many jobs to make them work on a part of the data.
	///
	/// The outer array has the size <see cref="outerSize"/>.x * <see cref="outerSize"/>.y * <see cref="outerSize"/>.z, laid out as if the coordinates were sorted by the tuple (Y,Z,X).
	/// The inner array has the size <see cref="slice.size"/>.x * <see cref="slice.size"/>.y * <see cref="slice.size"/>.z, also laid out as if the coordinates were sorted by the tuple (Y,Z,X).
	/// </summary>
	public readonly struct Slice3D {
		public readonly int3 outerSize;
		public readonly IntBounds slice;

		public Slice3D (IntBounds outer, IntBounds slice) : this(outer.size, slice.Offset(-outer.min)) {}

        public Slice3D (int3 outerSize, IntBounds slice) : this()
        {
        }

        public void AssertMatchesOuter<T>(UnsafeSpan<T> values) where T : unmanaged {
        }

        public void AssertMatchesOuter<T>(NativeArray<T> values) where T : struct {
        }

        public void AssertMatchesInner<T>(NativeArray<T> values) where T : struct {
        }

        public void AssertSameSize (Slice3D other) {
        }

        public int InnerCoordinateToOuterIndex (int x, int y, int z) {
            return default;
        }

        public int length => slice.size.x * slice.size.y * slice.size.z;

		public (int, int, int)outerStrides => (1, outerSize.x * outerSize.z, outerSize.x);
		public (int, int, int)innerStrides => (1, slice.size.x * slice.size.z, slice.size.x);
		public int outerStartIndex {
			get {
				var(dx, dy, dz) = outerStrides;
				return slice.min.x * dx + slice.min.y * dy + slice.min.z * dz;
			}
		}

		/// <summary>True if the slice covers the whole outer array</summary>
		public bool coversEverything => math.all(slice.size == outerSize);
	}

	/// <summary>Helpers for scheduling simple NativeArray jobs</summary>
	static class NativeArrayExtensions {
		/// <summary>this[i] = value</summary>
		public static JobMemSet<T> MemSet<T>(this NativeArray<T> self, T value) where T : unmanaged {
            return default;
        }

        /// <summary>this[i] &= other[i]</summary>
        public static JobAND BitwiseAndWith(this NativeArray<bool> self, NativeArray<bool> other)
        {
            return default;
        }

        /// <summary>to[i] = from[i]</summary>
        public static JobCopy<T> CopyToJob<T>(this NativeArray<T> from, NativeArray<T> to) where T : struct
        {
            return default;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static SliceActionJob<T> WithSlice<T>(this T action, Slice3D slice) where T : struct, GridIterationUtilities.ISliceAction
        {
            return default;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static IndexActionJob<T> WithLength<T>(this T action, int length) where T : struct, GridIterationUtilities.ISliceAction
        {
            return default;
        }

        public static JobRotate3DArray<T> Rotate3D<T>(this NativeArray<T> arr, int3 size, int dx, int dz) where T : unmanaged
        {
            return default;
        }
    }

	/// <summary>
	/// Treats input as a 3-dimensional array and copies it into the output at the specified position.
	///
	/// The <see cref="input"/> is a 3D array, and <see cref="inputSlice"/> refers to a rectangular slice of this array.
	/// The <see cref="output"/> is defined similarly.
	///
	/// The two slices must naturally have the same shape.
	/// </summary>
	[BurstCompile]
	public struct JobCopyRectangle<T> : IJob where T : struct {
		[ReadOnly]
		[DisableUninitializedReadCheck] // TODO: Fix so that job doesn't run instead
		public NativeArray<T> input;

		[WriteOnly]
		public NativeArray<T> output;

		public Slice3D inputSlice;
		public Slice3D outputSlice;

		public void Execute () {
        }

        /// <summary>
        /// Treats input as a 3-dimensional array and copies it into the output at the specified position.
        ///
        /// The input is a 3D array, and inputSlice refers to a rectangular slice of this array.
        /// The output is defined similarly.
        ///
        /// The two slices must naturally have the same shape.
        /// </summary>
        public static void Copy(NativeArray<T> input, NativeArray<T> output, Slice3D inputSlice, Slice3D outputSlice)
        {
        }
    }

	/// <summary>result[i] = value</summary>
	[BurstCompile]
	public struct JobMemSet<T> : IJob where T : unmanaged {
		[WriteOnly]
		public NativeArray<T> data;

		public T value;

		public void Execute() => data.AsUnsafeSpan().Fill(value);
	}

	/// <summary>to[i] = from[i]</summary>
	[BurstCompile]
	public struct JobCopy<T> : IJob where T : struct {
		[ReadOnly]
		public NativeArray<T> from;

		[WriteOnly]
		public NativeArray<T> to;

		public void Execute () {
        }
    }

	[BurstCompile]
	public struct IndexActionJob<T> : IJob where T : struct, GridIterationUtilities.ISliceAction {
		public T action;
		public int length;

		public void Execute () {
        }
    }

	[BurstCompile]
	public struct SliceActionJob<T> : IJob where T : struct, GridIterationUtilities.ISliceAction {
		public T action;
		public Slice3D slice;

		public void Execute () {
        }
    }

	/// <summary>result[i] &= data[i]</summary>
	public struct JobAND : GridIterationUtilities.ISliceAction {
		public NativeArray<bool> result;

		[ReadOnly]
		public NativeArray<bool> data;

		public void Execute (uint outerIdx, uint innerIdx) {
        }
    }

	[BurstCompile]
	public struct JobMaxHitCount : IJob {
		[ReadOnly]
		public NativeArray<RaycastHit> hits;
		public int maxHits;
		public int layerStride;
		[WriteOnly]
		public NativeArray<int> maxHitCount;
		public void Execute () {
        }
    }

	/// <summary>
	/// Copies hit points and normals.
	/// points[i] = hits[i].point (if anything was hit), normals[i] = hits[i].normal.normalized.
	/// </summary>
	[BurstCompile(FloatMode = FloatMode.Fast)]
	public struct JobCopyHits : IJob, GridIterationUtilities.ISliceAction {
		[ReadOnly]
		public NativeArray<RaycastHit> hits;

		[WriteOnly]
		public NativeArray<Vector3> points;

		[WriteOnly]
		public NativeArray<float4> normals;
		public Slice3D slice;

		public void Execute ()
        {
        }

        public void Execute(uint outerIdx, uint innerIdx)
        {
        }
    }

    [BurstCompile]
    public struct JobRotate3DArray<T> : IJob where T : unmanaged
    {
        public NativeArray<T> arr;
        public int3 size;
        public int dx, dz;

		public void Execute () {
        }
    }
}
