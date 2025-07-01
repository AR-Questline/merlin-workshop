using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Awaken.PackageUtilities.Collections {
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(ARUnsafeListDebugView<>))]
    public unsafe struct ARUnsafeList<T> where T : unmanaged {
        
        [NativeDisableUnsafePtrRestriction] T* _ptr;
        int _length;
        int _capacity;
        
        Allocator _allocator;
        
        public ARUnsafeList(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) {
            _ptr = null;
            _length = 0;
            _capacity = 0;
            _allocator = allocator;

            EnsureCapacity(math.max(initialCapacity, 1));

            if (options == NativeArrayOptions.ClearMemory && _ptr != null) {
                UnsafeUtility.MemClear(_ptr, _capacity * sizeof(T));
            }
        }

        public readonly T* Ptr => AllocationsTracker.Access(_ptr);
        
        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AwakenCollectionHelper.AssumePositive(_length);
            set {
                if (value > Capacity) {
                    Resize(value);
                } else {
                    _length = value;
                }
            }
        }
        
        public int Capacity {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AwakenCollectionHelper.AssumePositive(_capacity);
            set => SetCapacity(value);
        }

        public readonly bool IsCreated {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ptr != null;
        }
        
        /// <returns>If the list is empty or  has not been constructed.</returns>
        public readonly bool IsEmpty {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || _length == 0;
        }

        public ref T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                AwakenCollectionHelper.CheckIndexInBounds(index, _length);
                return ref *AllocationsTracker.Access(_ptr, AwakenCollectionHelper.AssumePositive(index));
            }
        }

        public void Dispose() {
            if (IsCreated && AwakenCollectionHelper.ShouldDeallocate(_allocator)) {
                AllocationsTracker.Free(_ptr, _allocator);
            }

            _ptr = null;
            _length = 0;
            _capacity = 0;
            _allocator = Allocator.Invalid;
        }

        public JobHandle Dispose(JobHandle dependencies) {
            if (IsCreated && AwakenCollectionHelper.ShouldDeallocate(_allocator)) {
                dependencies = new MemFreeJob(_ptr, _allocator).Schedule(dependencies);
            } 

            _ptr = null;
            _length = 0;
            _capacity = 0;
            _allocator = Allocator.Invalid;

            return dependencies;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T value) {
            var index = _length;
            _length++;
            if (_length > _capacity) {
                EnsureCapacity(_length);
            }
            *AllocationsTracker.Access(_ptr, index) = value;
        }
        
        public void AddRange(void* ptr, int count) {
            var index = _length;
            _length += count;
            if (_length > _capacity) {
                EnsureCapacity(_length);
            }
            UnsafeUtility.MemCpy(AllocationsTracker.Access(_ptr, index), ptr, count * sizeof(T));
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public void AddRange(in ARUnsafeList<T> list) {
            AddRange(list._ptr, list.Length);
        }

        public void AddReplicate(in T value, int count) {
            var index = _length;
            _length += count;
            if (_length > Capacity) {
                EnsureCapacity(_length);
            }
            fixed (void* ptr = &value) {
                UnsafeUtility.MemCpyReplicate(AllocationsTracker.Access(_ptr, index), ptr, sizeof(T), count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNoResize(T value) {
            AwakenCollectionHelper.CheckIndexInBounds(_length, _capacity);
            *AllocationsTracker.Access(_ptr, _length) = value;
            _length += 1;
        }

        public void AddRangeNoResize(void* ptr, int count) {
            AwakenCollectionHelper.CheckRangeInBounds(_length, count, _capacity);
            UnsafeUtility.MemCpy(AllocationsTracker.Access(_ptr, _length), AllocationsTracker.Access(_ptr), count * sizeof(T));
            _length += count;
        }

        public void AddRangeNoResize(ARUnsafeList<T> list) {
            AddRangeNoResize(list._ptr, AwakenCollectionHelper.AssumePositive(list.Length));
        }

        public void RemoveAtSwapBack(int index) {
            AwakenCollectionHelper.CheckIndexInBounds(index, _length);
            index = AwakenCollectionHelper.AssumePositive(index);
            int copyFrom = _length - 1;
            T* dst = AllocationsTracker.Access(_ptr, index);
            T* src = AllocationsTracker.Access(_ptr, copyFrom);
            (*dst) = (*src);
            _length -= 1;
        }

        public void RemoveRangeSwapBack(int index, int count) {
            AwakenCollectionHelper.CheckRangeInBounds(index, count, _length);

            index = AwakenCollectionHelper.AssumePositive(index);
            count = AwakenCollectionHelper.AssumePositive(count);

            if (count > 0) {
                int copyFrom = math.max(_length - count, index + count);
                var dst = AllocationsTracker.Access(_ptr, index);
                var src = AllocationsTracker.Access(_ptr, copyFrom);
                UnsafeUtility.MemCpy(dst, src, (_length - copyFrom) * sizeof(T));
                _length -= count;
            }
        }

        public void RemoveAt(int index) {
            AwakenCollectionHelper.CheckIndexInBounds(index, _length);

            index = AwakenCollectionHelper.AssumePositive(index);

            var dst =  AllocationsTracker.Access(_ptr, index);
            var src = AllocationsTracker.Access(dst, 1);
            _length--;
            
            UnsafeUtility.MemCpy(dst, src, (_length - index) * sizeof(T));
        }

        public void RemoveRange(int index, int count) {
            AwakenCollectionHelper.CheckRangeInBounds(index, count, _length);

            index = AwakenCollectionHelper.AssumePositive(index);
            count = AwakenCollectionHelper.AssumePositive(count);

            if (count > 0) {
                int copyFrom = math.min(index + count, _length);
                var dst = AllocationsTracker.Access(_ptr, index);
                var src = AllocationsTracker.Access(_ptr, copyFrom);
                UnsafeUtility.MemCpy(dst, src, (_length - copyFrom) * sizeof(T));
                _length -= count;
            }
        }

        public void InsertRange(int index, int count) => InsertRangeWithBeginEnd(index, index + count);

        public void InsertRangeWithBeginEnd(int begin, int end) {
            AwakenCollectionHelper.CheckBeginEndNoLength(begin, end);

            begin = AwakenCollectionHelper.AssumePositive(begin);
            end = AwakenCollectionHelper.AssumePositive(end);

            int items = end - begin;
            if (items < 1) {
                return;
            }

            var oldLength = _length;

            if (_length + items > Capacity) {
                Resize(_length + items);
            } else {
                _length += items;
            }

            var itemsToCopy = oldLength - begin;

            if (itemsToCopy < 1) {
                return;
            }

            var sizeOf = sizeof(T);
            var bytesToCopy = itemsToCopy * sizeOf;
            unsafe {
                byte* ptr = (byte*)Ptr;
                byte* dest = ptr + end * sizeOf;
                byte* src = ptr + begin * sizeOf;
                UnsafeUtility.MemMove(dest, src, bytesToCopy);
            }
        }

        public void Clear() {
            _length = 0;
        }
        
        /// <summary> sets the capacity to at least the specified value. </summary>
        /// <seealso cref="SetCapacity"/>
        public void EnsureCapacity(int capacity) {
            var newCapacity = math.max(capacity, CollectionHelper.CacheLineSize / sizeof(T));
            newCapacity = math.ceilpow2(newCapacity);
            SetCapacity(newCapacity);
        }

        /// <summary> sets the capacity to the specified value. </summary>
        /// <seealso cref="EnsureCapacity"/>
        public void SetCapacity(int capacity) {
            AwakenCollectionHelper.CheckCapacityInRange(capacity, Length);
            if (capacity == _capacity) {
                return;
            }
            T* newPtr = null;
            if (capacity > 0) {
                newPtr = AllocationsTracker.Malloc<T>((uint)capacity, _allocator);
                if (_ptr != null) {
                    UnsafeUtility.MemCpy(newPtr, _ptr, _length * sizeof(T));
                }
            }
            if (_ptr != null) {
                AllocationsTracker.Free(_ptr, _allocator);
            }
            _ptr = newPtr;
            _capacity = capacity;
        }
        
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) {
            var oldLength = this._length;

            if (length > Capacity) {
                SetCapacity(length);
            }

            this._length = length;

            if (options == NativeArrayOptions.ClearMemory && oldLength < length) {
                var num = length - oldLength;
                UnsafeUtility.MemClear(_ptr + oldLength, num * sizeof(T));
            }
        }

        /// <summary> Sets the capacity to match the length. </summary>
        public void TrimExcess() {
            if (Capacity != _length) {
                SetCapacity(_length);
            }
        }
        
        public void CopyFrom(T* ptr, int length) {
            Resize(length);
            UnsafeUtility.MemCpy(AllocationsTracker.Access(_ptr), ptr, sizeof(T) * length);
        }
        
        public void CopyFrom(in NativeArray<T> other) {
            CopyFrom((T*)other.GetUnsafeReadOnlyPtr(), other.Length);
        }

        public void CopyFrom(in ARUnsafeList<T> other) {
            CopyFrom(other._ptr, other.Length);
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(_ptr, _length);
        }
        
        public ReadOnly AsReadOnly() {
            return new ReadOnly(_ptr, Length);
        }

        public ParallelWriter AsParallelWriter() {
            AllocationsTracker.Access(_ptr);
            return new ParallelWriter((ARUnsafeList<T>*)UnsafeUtility.AddressOf(ref this));
        }
        
        public readonly struct ReadOnly {
            [NativeDisableUnsafePtrRestriction] readonly T* _ptr;
            readonly int _length;

            public T* Ptr {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => AllocationsTracker.Access(_ptr);
            }

            public ReadOnly(T* ptr, int length) {
                this._ptr = ptr;
                this._length = length;
            }
            
            public int Length {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _length;
            }
            
            public ref readonly T this[int index] {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    AwakenCollectionHelper.CheckIndexInBounds(index, _length);
                    return ref *AllocationsTracker.Access(_ptr, index);
                }
            }
            
            public ReadOnlyEnumerator GetEnumerator() {
                return new ReadOnlyEnumerator(AllocationsTracker.Access(_ptr), _length);
            }
        }

        public readonly struct ParallelWriter {
            [NativeDisableUnsafePtrRestriction] readonly ARUnsafeList<T>* _list;

            public ParallelWriter(ARUnsafeList<T>* list) {
                _list = list;
            }

            public void AddNoResize(T value) {
                var index = Interlocked.Increment(ref _list->_length) - 1;
                AwakenCollectionHelper.CheckIndexInBounds(index, _list->_capacity);
                *AllocationsTracker.Access(_list->_ptr, index) = value;
            }

            public void AddRangeNoResize(void* ptr, int count) {
                var index = Interlocked.Add(ref _list->_length, count) - count;
                AwakenCollectionHelper.CheckRangeInBounds(index, count, _list->_capacity);
                UnsafeUtility.MemCpy(AllocationsTracker.Access(_list->_ptr, index), ptr, count * sizeof(T));
            }
        }

        public ref struct Enumerator {
            readonly T* _ptr;
            readonly int _length;
            int _index;

            public Enumerator(T* ptr, int length) : this() {
                _ptr = ptr;
                _length = length;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _length;
            
            public ref T Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref *AllocationsTracker.Access(_ptr, _index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() => _index = -1;
            
            public void Dispose() { }
        }
        
        public ref struct ReadOnlyEnumerator {
            readonly T* _ptr;
            readonly int _length;
            int _index;

            public ReadOnlyEnumerator(T* ptr, int length) : this() {
                _ptr = ptr;
                _length = length;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _length;
            
            public ref readonly T Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref *AllocationsTracker.Access(_ptr, _index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() => _index = -1;
            
            public void Dispose() { }
        }
    }
    
    internal sealed class ARUnsafeListDebugView<T> where T : unmanaged {
        readonly ARUnsafeList<T> _data;

        public ARUnsafeListDebugView(ARUnsafeList<T> data) {
            _data = data;
        }

        public unsafe T[] Items {
            get {
                T[] result = new T[_data.Length];

                for (var i = 0; i < result.Length; ++i) {
                    result[i] = _data.Ptr[i];
                }

                return result;
            }
        }
    }
}