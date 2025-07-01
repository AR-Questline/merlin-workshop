using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Pathfinding.Pooling;

namespace Pathfinding.Util {
	/// <summary>Various utilities for handling arrays and memory</summary>
	public static class Memory {
		/// <summary>
		/// Returns a new array with at most length newLength.
		/// The array will contain a copy of all elements of arr up to but excluding the index newLength.
		/// </summary>
		public static T[] ShrinkArray<T>(T[] arr, int newLength) {
            return default;
        }

        /// <summary>Swaps the variables a and b</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(ref T a, ref T b)
        {
        }

        public static void Realloc<T>(ref NativeArray<T> arr, int newSize, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : struct
        {
        }

        public static void Realloc<T>(ref T[] arr, int newSize)
        {
        }

        public static T[] UnsafeAppendBufferToArray<T>(UnsafeAppendBuffer src) where T : unmanaged
        {
            return default;
        }

        public static void Rotate3DArray<T>(T[] arr, int3 size, int dx, int dz)
        {
        }
    }
}
