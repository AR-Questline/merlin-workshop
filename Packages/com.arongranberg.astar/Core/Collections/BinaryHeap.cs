// #define VALIDATE_BINARY_HEAP
#pragma warning disable 162
#pragma warning disable 429
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.CompilerServices;

namespace Pathfinding {
	using Pathfinding.Collections;

	/// <summary>
	/// Binary heap implementation.
	/// Binary heaps are really fast for ordering nodes in a way that
	/// makes it possible to get the node with the lowest F score.
	/// Also known as a priority queue.
	///
	/// This has actually been rewritten as a 4-ary heap
	/// for performance, but it's the same principle.
	///
	/// See: http://en.wikipedia.org/wiki/Binary_heap
	/// See: https://en.wikipedia.org/wiki/D-ary_heap
	/// </summary>
	[BurstCompile]
	public struct BinaryHeap {
		/// <summary>Internal backing array for the heap</summary>
		private UnsafeSpan<HeapNode> heap;

		/// <summary>Number of items in the tree</summary>
		public int numberOfItems;
		uint insertionOrder;

		/// <summary>Ties between elements that have the same F score can be broken by the H score or by insertion order</summary>
		public TieBreaking tieBreaking;

		public enum TieBreaking : byte {
			HScore,
			InsertionOrder,
		}

		/// <summary>The tree will grow by at least this factor every time it is expanded</summary>
		public const float GrowthFactor = 2;

		/// <summary>
		/// Number of children of each node in the tree.
		/// Different values have been tested and 4 has been empirically found to perform the best.
		/// See: https://en.wikipedia.org/wiki/D-ary_heap
		/// </summary>
		const int D = 4;

		public const ushort NotInHeap = 0xFFFF;

		/// <summary>True if the heap does not contain any elements</summary>
		public bool isEmpty => numberOfItems <= 0;

		/// <summary>Item in the heap</summary>
		private struct HeapNode {
			public uint pathNodeIndex;
			/// <summary>Bitpacked F and G scores</summary>
			public ulong sortKey;

			public HeapNode (uint pathNodeIndex, uint tieBreaker, uint f) : this()
            {
            }

            public uint F {
				get => (uint)(sortKey >> 32);
				set => sortKey = (sortKey & 0xFFFFFFFFUL) | ((ulong)value << 32);
			}

			public uint TieBreaker {
				get => (uint)sortKey;
				set => sortKey = (sortKey & 0xFFFFFFFF00000000UL) | (ulong)value;
			}
		}

		/// <summary>
		/// Rounds up v so that it has remainder 1 when divided by D.
		/// I.e it is of the form n*D + 1 where n is any non-negative integer.
		/// </summary>
		static int RoundUpToNextMultipleMod1 (int v) {
            return default;
        }

        /// <summary>Create a new heap with the specified initial capacity</summary>
        public BinaryHeap(int capacity) : this()
        {
        }

        public void Dispose()
        {
        }

        /// <summary>Removes all elements from the heap</summary>
        public void Clear(UnsafeSpan<PathNode> pathNodes)
        {
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public uint GetPathNodeIndex(int heapIndex) => heap[heapIndex].pathNodeIndex;

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public uint GetH(int heapIndex) => tieBreaking == TieBreaking.HScore ? heap[heapIndex].TieBreaker : 0;

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public uint GetF(int heapIndex) => heap[heapIndex].F;

		/// <summary>
		/// Replaces the H score of a node in the heap.
		///
		/// Warning: Assumes that ties are broken by H scores.
		/// </summary>
		public void SetH (int heapIndex, uint h) {
        }

        /// <summary>Expands to a larger backing array when the current one is too small</summary>
        static void Expand (ref UnsafeSpan<HeapNode> heap) {
        }

        /// <summary>Adds a node to the heap</summary>
        public void Add (UnsafeSpan<PathNode> nodes, uint pathNodeIndex, uint g, uint h) {
        }

        [BurstCompile]
		static void Add (ref BinaryHeap binaryHeap, ref UnsafeSpan<PathNode> nodes, uint pathNodeIndex, uint g, uint h, uint insertionOrder, TieBreaking tieBreaking) {
        }

        static void DecreaseKey(UnsafeSpan<HeapNode> heap, UnsafeSpan<PathNode> nodes, HeapNode node, ushort index)
        {
        }

        /// <summary>
        /// Returns the node with the lowest F score from the heap.
        ///
        /// Note: If <see cref="tieBreaking"/> is set not set to HScore, the returned h score will be 0.
        /// </summary>
        public uint Remove(UnsafeSpan<PathNode> nodes, out uint g, out uint h)
        {
            g = default(uint);
            h = default(uint);
            return default;
        }

        [BurstCompile]
		static uint Remove (ref UnsafeSpan<PathNode> nodes, ref BinaryHeap binaryHeap, [NoAlias] out uint removedTieBreaker, [NoAlias] out uint removedF) {
            removedTieBreaker = default(uint);
            removedF = default(uint);
            return default;
        }

        [System.Diagnostics.Conditional("VALIDATE_BINARY_HEAP")]
        static void Validate(ref UnsafeSpan<PathNode> nodes, ref BinaryHeap binaryHeap)
        {
        }

        /// <summary>
        /// Rebuilds the heap by trickeling down all items.
        /// Usually called after the hTarget on a path has been changed
        /// </summary>
        public void Rebuild(UnsafeSpan<PathNode> nodes)
        {
        }
    }
}
