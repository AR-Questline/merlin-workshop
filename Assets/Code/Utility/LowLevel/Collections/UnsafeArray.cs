using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Debugging;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;

namespace Awaken.Utility.LowLevel.Collections {
    [DebuggerDisplay("Length = {Length}, IsCreated = {IsCreated}")]
    [DebuggerTypeProxy(typeof(UnsafeArray<>.UnsafeArrayTDebugView))]
    public unsafe struct UnsafeArray<T> where T : unmanaged {
        [NativeDisableUnsafePtrRestriction] T* _array;
        readonly uint _length;
        readonly Allocator _allocator;

        public uint Length => _length;
        public int LengthInt => (int)_length;
        public bool IsCreated => _array != null;

        public ref T this[uint index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                Asserts.IndexInRange(index, _length);
                return ref *AllocationsTracker.Access(_array, index);
            }
        }

        public readonly T* Ptr => AllocationsTracker.Access(_array);
        public readonly Allocator Allocator => _allocator;

        public UnsafeArray(uint length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory) {
            _length = length;
            _allocator = allocator;

            _array = AllocationsTracker.Malloc<T>(_length, _allocator);

            if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory) {
                return;
            }
            UnsafeUtility.MemClear(_array, this.Length * UnsafeUtility.SizeOf<T>());
        }

        public UnsafeArray(T[] array, Allocator allocator) : this((uint)array.Length, allocator, NativeArrayOptions.UninitializedMemory) {
            fixed (void* ptr = array) {
                UnsafeUtility.MemCpy(_array, ptr, _length * UnsafeUtility.SizeOf<T>());
            }
        }

        public UnsafeArray(T[] array, uint length, Allocator allocator) : this(length, allocator, NativeArrayOptions.UninitializedMemory) {
            Asserts.IsLessOrEqual((uint)array.Length, length);
            fixed (void* ptr = array) {
                UnsafeUtility.MemCpy(_array, ptr, _length * UnsafeUtility.SizeOf<T>());
            }
        }

        public UnsafeArray(UnsafeList<T> list, Allocator allocator) : this((uint)list.Length, allocator, NativeArrayOptions.UninitializedMemory) {
            UnsafeUtility.MemCpy(_array, list.Ptr, _length * UnsafeUtility.SizeOf<T>());
        }

        public UnsafeArray(UnsafeArray<T> other, Allocator allocator) : this(other.Length, allocator, NativeArrayOptions.UninitializedMemory) {
            UnsafeUtility.MemCpy(_array, other.Ptr, _length * UnsafeUtility.SizeOf<T>());
        }

        UnsafeArray(T* backingArray, uint length, Allocator allocator) {
            _length = length;
            _allocator = allocator;
            _array = backingArray;
        }

        public void Dispose() {
#if AR_DEBUG || UNITY_EDITOR
            if (_allocator == Allocator.Invalid) {
                UnityEngine.Debug.LogError($"Calling Dispose on already Disposed {nameof(UnsafeArray<T>)}");
            }
#endif
            if (_allocator > Allocator.None) {
                AllocationsTracker.Free(_array, _allocator);
            }
            this = default;
        }

        public JobHandle Dispose(JobHandle dependencies) {
#if AR_DEBUG || UNITY_EDITOR
            if (_allocator == Allocator.Invalid) {
                UnityEngine.Debug.LogError($"Calling Dispose on already Disposed {nameof(UnsafeArray<T>)}");
            }
#endif
            if (_allocator > Allocator.None) {
                var job = new DisposeJob {
                    array = _array,
                    allocator = _allocator
                };
                this = default;
                return job.Schedule(dependencies);
            }

            return dependencies;
        }

        public Enumerator GetEnumerator() {
            return new(this);
        }

        public static UnsafeArray<T>.Span FromExistingData(T* data, uint length) {
            return new UnsafeArray<T>.Span(data, length);
        }

        // From native array
        public ref U ReinterpretLoad<U>(uint sourceIndex) where U : unmanaged {
            CheckReinterpretRange<U>(sourceIndex);

            var startPtr = AllocationsTracker.Access(_array, sourceIndex);
            return ref *(U*)startPtr;
        }

        // From native array
        public void ReinterpretStore<U>(uint destIndex, in U data) where U : unmanaged {
            CheckReinterpretRange<U>(destIndex);

            var startPtr = AllocationsTracker.Access(_array, destIndex);
            *(U*)startPtr = data;
        }

        public void Fill(T value) {
            UnsafeUtility.MemCpyReplicate(_array, &value, UnsafeUtility.SizeOf<T>(), (int)_length);
        }

        public UnsafeArray<TU> Move<TU>() where TU : unmanaged {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<TU>()) {
                throw new InvalidOperationException($"Types {typeof(T)} and {typeof(TU)} are different sizes - direct reinterpretation is not possible");
            }
#endif
            var result = new UnsafeArray<TU>((TU*)AllocationsTracker.Access(_array), _length, _allocator);
            _array = null;
            return result;
        }

        public UnsafeArray<TU>.Span Reinterpret<TU>() where TU : unmanaged {
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<TU>()) {
                throw new InvalidOperationException($"Types {typeof(T)} and {typeof(TU)} are different sizes - direct reinterpretation is not possible");
            }
#endif
            return UnsafeArray<TU>.FromExistingData((TU*)AllocationsTracker.Access(_array), _length);
        }

        public UnsafeArray<T>.Span AsSlice(uint start, uint length) {
            return new Span(AllocationsTracker.Access(_array, start), length);
        }
        
        public Span<T> AsSpan() {
            return AsSpan(0, _length);
        }
        
        public Span<T> AsSpan(uint start, uint length) {
            return new Span<T>(AllocationsTracker.Access(_array, start), (int)length);
        }
        
        public UnsafeArray<TU>.Span As<TU>() where TU : unmanaged {
            var bytes = _length * (uint)UnsafeUtility.SizeOf<T>();
            var resultLength = bytes / (uint)UnsafeUtility.SizeOf<TU>();

#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            if (bytes != UnsafeUtility.SizeOf<TU>() * resultLength) {
                throw new InvalidOperationException($"{bytes} bytes from type {typeof(T)} can not be converted to type {typeof(TU)}");
            }
#endif
            return UnsafeArray<TU>.FromExistingData((TU*)AllocationsTracker.Access(_array), resultLength);
        }

        public NativeArray<T> AsNativeArray() {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(_array, (int)_length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var atomicSafetyHandler = AtomicSafetyHandle.GetTempMemoryHandle();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, atomicSafetyHandler);
#endif
            return array;
        }

        public NativeArray<T> ToNativeArray(Allocator allocator) {
            return new NativeArray<T>(AsNativeArray(), allocator);
        }

        public readonly T[] ToManagedArray() {
            var array = new T[_length];
            fixed (T* arrayPtr = array) {
                UnsafeUtility.MemCpy(arrayPtr, Ptr, _length * UnsafeUtility.SizeOf<T>());
            }
            return array;
        }

        public static implicit operator UnsafeArray<T>.Span(UnsafeArray<T> array) {
            if (array.IsCreated) {
                return FromExistingData(AllocationsTracker.Access(array._array), array._length);
            } else {
                return FromExistingData(array._array, array._length);
            }
        }

        public static void Resize(ref UnsafeArray<T> array, uint newLength, NativeArrayOptions options = NativeArrayOptions.ClearMemory) {
            var newArray = new UnsafeArray<T>(newLength, array._allocator);
            var copyLength = math.min(array._length, newLength);
            UnsafeUtility.MemCpy(newArray._array, array._array, copyLength * UnsafeUtility.SizeOf<T>());
            array.Dispose();
            array = newArray;

            var clearLength = newLength - copyLength;
            if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory | clearLength < 1) {
                return;
            }
            UnsafeUtility.MemClear(newArray._array + copyLength, clearLength * UnsafeUtility.SizeOf<T>());
        }

        // From native array
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("DEBUG"), Conditional("AR_DEBUG")]
        void CheckReinterpretRange<U>(uint sourceIndex) where U : struct {
            long bytesSize = _length * UnsafeUtility.SizeOf<T>();
            long bytesStartRange = sourceIndex * UnsafeUtility.SizeOf<T>();
            long bytesEndRange = bytesStartRange + UnsafeUtility.SizeOf<U>();
            if (bytesEndRange > bytesSize) {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), "byte range must fall inside container bounds");
            }
        }

        [Serializable, Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public ref struct Enumerator {
            readonly T* _array;
            readonly uint _length;
            uint _index;

            internal Enumerator(UnsafeArray<T> array) {
                _array = AllocationsTracker.Access(array._array);
                _length = array._length;
                _index = uint.MaxValue;
            }

            internal Enumerator(UnsafeArray<T>.Span array) {
                _array = AllocationsTracker.Access(array.Ptr);
                _length = array.Length;
                _index = uint.MaxValue;
            }

            public void Dispose() { }

            public bool MoveNext() {
                unchecked {
                    return ++_index < _length;
                }
            }

            public ref T Current => ref *AllocationsTracker.Access(_array, _index);
        }

        [DebuggerDisplay("Length = {Length}, IsValid = {IsValid}")]
        [DebuggerTypeProxy(typeof(UnsafeArray<>.Span.SpanTDebugView))]
        public readonly struct Span {
            [NativeDisableUnsafePtrRestriction] readonly T* _array;
            readonly uint _length;

            public T* Ptr => AllocationsTracker.Access(_array);
            public uint Length => _length;
            public int LengthInt => (int)_length;
            public bool IsValid => _array != null;

            public Span(T* array, uint length) {
                _array = array;
                _length = length;
            }

            public ref T this[uint index] {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    Asserts.IndexInRange(index, _length);
                    return ref *AllocationsTracker.Access(_array, index);
                }
            }

            public Enumerator GetEnumerator() {
                return new(this);
            }

            public UnsafeArray<T> AsUnsafeArray() {
                return new UnsafeArray<T>(AllocationsTracker.Access(_array), _length, Allocator.None);
            }

            public NativeArray<T> AsNativeArray() {
                return AsUnsafeArray().AsNativeArray();
            }

            public static explicit operator UnsafeArray<T>(UnsafeArray<T>.Span span) {
                return span.AsUnsafeArray();
            }

            sealed class SpanTDebugView {
                Span _data;

                public SpanTDebugView(Span data) {
                    _data = data;
                }

                public T[] Items {
                    get {
                        if (!_data.IsValid) {
                            return Array.Empty<T>();
                        }
                        return _data.AsUnsafeArray().ToManagedArray();
                    }
                }
            }
        }

        public readonly ref struct ScopeGuard {
            readonly UnsafeArray<T>* _array;

            public ScopeGuard(ref UnsafeArray<T> array) {
                _array = (UnsafeArray<T>*)UnsafeUtility.AddressOf(ref array);
            }

            public void Dispose() {
                _array->Dispose();
            }
        }

        sealed class UnsafeArrayTDebugView {
            UnsafeArray<T> _data;

            public UnsafeArrayTDebugView(UnsafeArray<T> data) {
                _data = data;
            }

            public T[] Items {
                get {
                    if (!_data.IsCreated) {
                        return Array.Empty<T>();
                    }
                    return _data.ToManagedArray();
                }
            }
        }

        struct DisposeJob : IJob {
            [NativeDisableUnsafePtrRestriction] public T* array;
            public Allocator allocator;

            public void Execute() {
                AllocationsTracker.Free(array, allocator);
            }
        }
    }
}
