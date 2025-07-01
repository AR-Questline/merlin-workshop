using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;

namespace Awaken.PackageUtilities.Collections {
    /// <summary> Copies of methods from Unity.Collections.CollectionHelper because they are internal </summary>
    public static class AwakenCollectionHelper {
        [return: AssumeRange(0, int.MaxValue)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AssumePositive(int value) => value;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckIndexInBounds(int index, int length) {
            // This checks both < 0 and >= Length with one comparison
            if ((uint)index >= (uint)length) {
                throw new IndexOutOfRangeException($"Index {index} is out of range in container of '{length}' Length.");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckRangeInBounds(int index, int count, int length) {
            if (count < 0) {
                throw new ArgumentOutOfRangeException($"Value for count {count} must be positive.");
            }

            if (index < 0) {
                throw new IndexOutOfRangeException($"Value for index {index} must be positive.");
            }

            if (index + count > length) {
                throw new ArgumentOutOfRangeException($"Value for count {count} is out of bounds.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckCapacityInRange(int capacity, int length) {
            if (capacity < 0) {
                throw new ArgumentOutOfRangeException($"Capacity {capacity} must be positive.");
            }
            if (capacity < length) {
                throw new ArgumentOutOfRangeException($"Capacity {capacity} is out of range in container of '{length}' Length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void CheckBeginEndNoLength(int begin, int end)
        {
            if (begin > end)
            {
                throw new ArgumentException($"Value for begin {begin} index must be less or equal to end {end}.");
            }

            if (begin < 0)
            {
                throw new ArgumentOutOfRangeException($"Value for begin {begin} must be positive.");
            }
        }
        
        public static bool ShouldDeallocate(AllocatorManager.AllocatorHandle allocator) {
            // Allocator.Invalid == container is not initialized.
            // Allocator.None    == container is initialized, but container doesn't own data.
            return allocator.ToAllocator > Allocator.None;
        }
    }
}