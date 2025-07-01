using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Awaken.Utility.Collections {
    public static class NativeCollectionsExt {
        public static int SafeCapacity(this in NativeBitArray collection) {
            if (collection.IsCreated) {
                return collection.Capacity;
            }

            return 0;
        }

        public static int SafeBucketsLength(this in UnsafeBitmask collection) {
            if (collection.IsCreated) {
                return collection.BucketsLength;
            }

            return 0;
        }

        public static uint SafeElementsLength(this in UnsafeBitmask collection) {
            if (collection.IsCreated) {
                return collection.ElementsLength;
            }

            return 0;
        }

        public static int SafeCapacity<T>(this in NativeList<T> collection) where T : unmanaged {
            if (collection.IsCreated) {
                return collection.Capacity;
            }

            return 0;
        }

        public static int SafeCapacity<T>(this in UnsafeList<T> collection) where T : unmanaged {
            if (collection.IsCreated) {
                return collection.Capacity;
            }

            return 0;
        }

        public static int SafeCapacity<T>(this in ARUnsafeList<T> collection) where T : unmanaged {
            if (collection.IsCreated) {
                return collection.Capacity;
            }

            return 0;
        }

        public static int SafeLength<T>(this in NativeList<T> collection) where T : unmanaged {
            if (collection.IsCreated) {
                return collection.Length;
            }

            return 0;
        }

        public static int SafeLength<T>(this in NativeArray<T> collection) where T : unmanaged {
            if (collection.IsCreated) {
                return collection.Length;
            }

            return 0;
        }

        public static int FindIndexOf<T, U>(this in NativeList<T> list, in U search, int startIndex = 0)
            where T : unmanaged
            where U : unmanaged, IEquatable<T> {
            for (var i = startIndex; i < list.Length; i++) {
                if (search.Equals(list[i])) {
                    return i;
                }
            }

            return -1;
        }
        
        public static int FindIndexOf<T, U>(this in NativeArray<T> array, in U search, int startIndex = 0)
            where T : unmanaged
            where U : unmanaged, IEquatable<T> {
            for (var i = startIndex; i < array.Length; i++) {
                if (search.Equals(array[i])) {
                    return i;
                }
            }

            return -1;
        }

        public static int FindIndexOf<T, U>(this in UnsafeList<T> list, in U search, int startIndex = 0)
            where T : unmanaged
            where U : unmanaged, IEquatable<T> {
            for (var i = startIndex; i < list.Length; i++) {
                if (search.Equals(list[i])) {
                    return i;
                }
            }
            return -1;
        }

        public static int FindIndexOf<T, U>(this in ARUnsafeList<T> list, in U search, int startIndex = 0)
            where T : unmanaged
            where U : unmanaged, IEquatable<T> {
            for (var i = startIndex; i < list.Length; i++) {
                if (search.Equals(list[i])) {
                    return i;
                }
            }
            return -1;
        }

        public static int FindIndexOf<T, U>(this in UnsafeArray<T> array, in U search, uint startIndex = 0)
            where T : unmanaged
            where U : unmanaged, IEquatable<T> {
            for (var i = startIndex; i < array.Length; i++) {
                if (search.Equals(array[i])) {
                    return (int)i;
                }
            }

            return -1;
        }

        [BurstCompile]
        public struct FindIndexJob<T, U> : IJob where T : unmanaged where U : unmanaged, IEquatable<T> {
            [ReadOnly] public NativeArray<T> array;
            public U search;
            public NativeReference<int> output;

            public void Execute() {
                output.Value = -1;
                for (var index = 0; index < array.Length; index++) {
                    if (search.Equals(array[index])) {
                        output.Value = index;
                        break;
                    }
                }
            }
        }

        public static unsafe void Sort<T, U>(in UnsafeArray<T> array, U comparer) where T : unmanaged where U : IComparer<T> {
            NativeSortExtension.Sort(array.Ptr, (int)array.Length, comparer);
        }

        public static unsafe void Sort<T>(in UnsafeArray<T> array) where T : unmanaged, IComparable<T> {
            NativeSortExtension.Sort(array.Ptr, (int)array.Length, new NativeSortExtension.DefaultComparer<T>());
        }

        public static void EnsureLength<T>(this ref NativeList<T> list, int length) where T : unmanaged {
            if (list.Length < length) {
                list.Resize(length, NativeArrayOptions.ClearMemory);
            }
        }

        public static void EnsureLengthWithFillNewCapacity<T>(this ref NativeList<T> list, int length, T fillValue) where T : unmanaged {
            if (list.Length < length) {
                var prevCapacity = list.Capacity;
                list.Resize(length, NativeArrayOptions.UninitializedMemory);
                if (list.Capacity <= prevCapacity) {
                    return;
                }

                list.FillUpToCapacity(prevCapacity, fillValue);
            }
        }

        public static unsafe void FillUpToCapacity<T>(this ref NativeList<T> list, int startIndex, T fillValue) where T : unmanaged {
            var capacity = list.Capacity;
            if (startIndex < 0 || startIndex >= capacity) {
                CollectionsLogs.LogErrorIndexIsOutOfRangeForCapacity(startIndex, capacity);
                return;
            }

            int sizeOfT = UnsafeUtility.SizeOf<T>();
            void* destPtr = ((byte*)list.GetUnsafePtr()) + (sizeOfT * startIndex);
            T* fillValuePtr = &fillValue;
            var elementsCount = capacity - startIndex;
            UnsafeUtility.MemCpyReplicate(destPtr, fillValuePtr, sizeOfT, elementsCount);
        }

        public static unsafe T[] ToArray<T>(this in ARUnsafeList<T>.ReadOnly list) where T : unmanaged {
            T[] dst = new T[list.Length];
            GCHandle gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy(gcHandle.AddrOfPinnedObject().ToPointer(), list.Ptr, list.Length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
            return dst;
        }

        public static unsafe UnsafeArray<T> ToUnsafeArray<T>(this in UnsafeList<T> list, Allocator allocator) where T : unmanaged {
            var array = new UnsafeArray<T>((uint)list.Length, allocator);
            UnsafeUtility.MemCpy(array.Ptr, list.Ptr, list.Length * UnsafeUtility.SizeOf<T>());
            return array;
        }

        public static unsafe UnsafeArray<T> ToUnsafeArray<T>(this in UnsafeArray<T>.Span span, Allocator allocator) where T : unmanaged {
            var array = new UnsafeArray<T>(span.Length, allocator);
            UnsafeUtility.MemCpy(array.Ptr, span.Ptr, span.Length * UnsafeUtility.SizeOf<T>());
            return array;
        }

        public static unsafe UnsafeArray<T> ToUnsafeArray<T>(this in UnsafeArray<T> oldArray, Allocator allocator) where T : unmanaged {
            var array = new UnsafeArray<T>(oldArray.Length, allocator);
            UnsafeUtility.MemCpy(array.Ptr, oldArray.Ptr, oldArray.Length * UnsafeUtility.SizeOf<T>());
            return array;
        }

        public static unsafe UnsafeArray<T>.Span AsUnsafeSpan<T>(this in ARUnsafeList<T>.ReadOnly list) where T : unmanaged {
            return UnsafeArray<T>.FromExistingData(list.Ptr, (uint)list.Length);
        }

        public static unsafe UnsafeArray<T>.Span AsUnsafeSpan<T>(this in ARUnsafeList<T> list) where T : unmanaged {
            return UnsafeArray<T>.FromExistingData(list.Ptr, (uint)list.Length);
        }

        public static unsafe UnsafeArray<T>.Span AsUnsafeSpan<T>(this in NativeArray<T> array) where T : unmanaged {
            return new UnsafeArray<T>.Span((T*)array.GetUnsafePtr(), (uint)array.Length);
        }

        public static unsafe ReadOnlySpan<byte> AsByteSpan<T>(this in NativeArray<T> array) where T : unmanaged {
            var bytesCount = array.Length * UnsafeUtility.SizeOf<T>();
            return new ReadOnlySpan<byte>(array.GetUnsafePtr(), bytesCount);
        }

        public static unsafe ReadOnlySpan<byte> AsByteSpan<T>(this in NativeList<T> list) where T : unmanaged {
            var bytesCount = list.Length * UnsafeUtility.SizeOf<T>();
            return new ReadOnlySpan<byte>(list.GetUnsafePtr(), bytesCount);
        }

        public static unsafe ReadOnlySpan<byte> AsByteSpan<T>(this in UnsafeArray<T> array) where T : unmanaged {
            var bytesCount = (int)(array.Length * UnsafeUtility.SizeOf<T>());
            return new ReadOnlySpan<byte>(array.Ptr, bytesCount);
        }

        public static unsafe Span<T> AsSystemSpan<T>(this in UnsafeArray<T> array) where T : unmanaged {
            return new Span<T>(array.Ptr, (int)array.Length);
        }

        public static unsafe UnsafeList<T> ToUnsafeList<T>(this in UnsafeArray<T>.Span span, Allocator allocator) where T : unmanaged {
            var list = new UnsafeList<T>(span.LengthInt, allocator);
            UnsafeUtility.MemCpy(list.Ptr, span.Ptr, span.Length * UnsafeUtility.SizeOf<T>());
            return list;
        }

        public static unsafe UnsafeList<T> ToUnsafeList<T>(this in UnsafeArray<T> array, Allocator allocator) where T : unmanaged {
            var list = new UnsafeList<T>(array.LengthInt, allocator);
            UnsafeUtility.MemCpy(list.Ptr, array.Ptr, array.Length * UnsafeUtility.SizeOf<T>());
            return list;
        }

        public static NativeArray<T> AsNativeArray<T>(this UnsafeList<T> list) where T : unmanaged {
            return list.AsNativeArray(0, list.Length);
        }

        public static unsafe NativeArray<T> AsNativeArray<T>(this UnsafeList<T> list, int startIndex, int length) where T : unmanaged {
            return ConvertExistingDataToNativeArray<T>(list.Ptr + startIndex, length);
        }
        
        public static unsafe NativeArray<T> ConvertExistingDataToNativeArray<T>(T* ptr, int length) where T : unmanaged {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var atomicSafetyHandler = AtomicSafetyHandle.GetTempMemoryHandle();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, atomicSafetyHandler);
#endif
            return array;
        }

        public static unsafe int ThreadSafeAddNoResize<T>(this ref UnsafeList<T> list, T value) where T : unmanaged {
            var idx = Interlocked.Increment(ref list.m_length) - 1;
            UnsafeUtility.WriteArrayElement(list.Ptr, idx, value);
            return idx;
        }

        public static unsafe int ThreadSafeAddNoResize<T>(this ref NativeList<T> list, T value) where T : unmanaged {
            var idx = Interlocked.Increment(ref list.GetUnsafeList()->m_length) - 1;
            UnsafeUtility.WriteArrayElement(list.GetUnsafeList()->Ptr, idx, value);
            return idx;
        }

        public static void Resize<T>(this ref NativeArray<T> array, int newSize, Allocator allocator, NativeArrayOptions nativeArrayOptions = NativeArrayOptions.ClearMemory) where T : unmanaged {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(newSize > array.Length);
#endif
            var arrayCopy = new NativeArray<T>(newSize, allocator, nativeArrayOptions);
            arrayCopy.GetSubArray(0, array.Length).CopyFrom(array);
            array = arrayCopy;
        }

        public static unsafe void Resize<T>(this ref UnsafeArray<T> array, uint newSize, NativeArrayOptions nativeArrayOptions = NativeArrayOptions.ClearMemory) where T : unmanaged {
            var copyCount = math.min(array.Length, newSize);
            var arrayCopy = new UnsafeArray<T>(newSize, array.Allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(arrayCopy.Ptr, array.Ptr, copyCount * UnsafeUtility.SizeOf<T>());
            if (((nativeArrayOptions & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory) & newSize > array.Length) {
                UnsafeUtility.MemClear(arrayCopy.Ptr + array.Length, (newSize - array.Length) * UnsafeUtility.SizeOf<T>());
            }
            array = arrayCopy;
        }

        public static unsafe void Resize<T>(this ref UnsafeArray<T> array, uint newSize, T fillValue) where T : unmanaged {
            var copyCount = math.min(array.Length, newSize);
            var arrayCopy = new UnsafeArray<T>(newSize, array.Allocator, NativeArrayOptions.UninitializedMemory);
            var sizeOfT = UnsafeUtility.SizeOf<T>();

            UnsafeUtility.MemCpy(arrayCopy.Ptr, array.Ptr, copyCount * sizeOfT);

            if (array.Length < newSize) {
                void* destPtr = arrayCopy.Ptr + array.Length;
                T* fillValuePtr = &fillValue;
                var fillCount = (int)(newSize - array.Length);
                UnsafeUtility.MemCpyReplicate(destPtr, fillValuePtr, sizeOfT, fillCount);
            }

            array = arrayCopy;
        }

        public static NativeArray<T> CreateCopy<T>(this NativeArray<T> array, Allocator allocator) where T : unmanaged {
            var arrayCopy = new NativeArray<T>(array.Length, allocator, NativeArrayOptions.UninitializedMemory);
            arrayCopy.CopyFrom(array);
            return arrayCopy;
        }

        public static void EnsureCapacity<T>(this NativeList<T> list, int capacity) where T : unmanaged {
            if (list.Capacity < capacity) {
                list.SetCapacity(capacity);
            }
        }

        public static float Sum(this UnsafeList<float> list) {
            var sum = 0f;
            for (var i = 0; i < list.Length; i++) {
                sum += list[i];
            }
            return sum;
        }

        public static float Avg(this UnsafeList<float> list) {
            return list.Sum() / list.Length;
        }

        public static void DisposeIfCreated<T>(ref this UnsafeArray<T> array) where T : unmanaged {
            if (array.IsCreated) {
                array.Dispose();
            }
        }

        public static void DisposeIfCreated(ref this UnsafeBitmask bitmask) {
            if (bitmask.IsCreated) {
                bitmask.Dispose();
            }
        }

        public static bool SequenceEqual<T>(this in UnsafeArray<T> array, in UnsafeArray<T> other) where T : unmanaged, IEquatable<T> {
            if (!array.IsCreated) {
                return !other.IsCreated;
            }
            if (array.Length != other.Length) {
                return false;
            }
            for (var i = 0u; i < array.Length; i++) {
                if (!array[i].Equals(other[i])) {
                    return false;
                }
            }
            return true;
        }

        public static int SequenceHashCode<T>(this in UnsafeArray<T> array) where T : unmanaged {
            if (!array.IsCreated) {
                return 0;
            }
            if (array.Length == 0) {
                return 0;
            }
            var hash = (int)array.Allocator;
            unchecked {
                for (var i = 0u; i < array.Length; i++) {
                    hash = (hash * 397) ^ array[i].GetHashCode();
                }
            }
            return hash;
        }

        public static T GetFirstAndOnlyEntry<T>(this NativeHashSet<T> hashSet) where T : unmanaged, IEquatable<T> {
            if (hashSet.Count != 1) {
                Log.Important?.Error($"Cannot call {nameof(GetFirstAndOnlyEntry)} on hashset where count != 1");
                return default;
            }
            foreach (var value in hashSet) {
                return value;
            }
            return default;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue = default) 
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged, IEquatable<TValue> {
            if (hashMap.TryGetValue(key, out var value) == false) {
                value = defaultValue;
            }
            return value;
        }
        
        public static void AddRange<T>(this ref NativeHashSet<T> hashSet, NativeHashSet<T> other) where T : unmanaged, IEquatable<T> {
            if (other.Count == 0) {
                return;
            }
            foreach (var value in other) {
                hashSet.Add(value);
            }
        }

        public static unsafe void CopyFrom<T>(this ref UnsafeList<T> list, ReadOnlySpan<T> span) where T : unmanaged {
            list.Resize(span.Length);
            fixed (T* ptr = span) {
                UnsafeUtility.MemCpy(list.Ptr, ptr, UnsafeUtility.SizeOf<T>() * span.Length);
            }
        }

        public static unsafe UnsafeArray<T> Contact<T>(this in UnsafeArray<T> left, in UnsafeArray<T>.Span right, Allocator allocator) where T : unmanaged {
            var result = new UnsafeArray<T>(left.Length + right.Length, allocator);
            var resultPtr = result.Ptr;
            UnsafeUtility.MemCpy(resultPtr, left.Ptr, left.Length * UnsafeUtility.SizeOf<T>());
            resultPtr += left.Length;
            UnsafeUtility.MemCpy(resultPtr, right.Ptr, right.Length * UnsafeUtility.SizeOf<T>());
            return result;
        }

        public static unsafe PinnedObject AsUnsafe<T>(this T[] managed, out UnsafeArray<T>.Span span) where T : unmanaged {
            var unsafeArray = (T*)UnsafeUtility.PinGCArrayAndGetDataAddress(managed, out var gcHandle);
            span = new UnsafeArray<T>.Span(unsafeArray, (uint)managed.Length);
            return new PinnedObject(gcHandle);
        }

        public static bool RemoveSwapBack<T, U>(this UnsafeList<T> list, U element) where T : unmanaged where U : unmanaged, IEquatable<T> {
            int count = list.Length;
            for (int i = 0; i < count; i++) {
                if (element.Equals(list[i])) {
                    list.RemoveAtSwapBack(i);
                    return true;
                }
            }
            return false;
        }

        public readonly ref struct PinnedObject {
            public readonly ulong gcHandle;

            public PinnedObject(ulong gcHandle) {
                this.gcHandle = gcHandle;
            }

            [UnityEngine.Scripting.Preserve] 
            public void Dispose() {
                UnsafeUtility.ReleaseGCObject(gcHandle);
            }
        }
    }
}