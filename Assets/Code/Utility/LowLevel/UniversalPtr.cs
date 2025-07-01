using Awaken.Utility.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Awaken.Utility.LowLevel {
    public unsafe struct UniversalPtr {
        const Allocator Allocator = ARAlloc.Persistent;
        
        public object obj;
        public void* ptr;
        
        UniversalPtr(object obj) {
            ptr = null;
            this.obj = obj;
        }
        
        UniversalPtr(void* ptr) {
            obj = null;
            this.ptr = ptr;
        }
        
        public static UniversalPtr CreateManaged<T>(in T value) where T : class {
            return new UniversalPtr(value);
        }

        public static UniversalPtr CreateUnmanaged<T>(in T value = default) where T : unmanaged {
            var ptr = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), Allocator);
            *(T*)ptr = value;
            return new UniversalPtr(ptr);
        }
        
        public readonly T GetManaged<T>() {
            return (T)obj;
        }
        
        public readonly ref T GetUnmanaged<T>() where T : unmanaged {
            return ref *(T*)ptr;
        }

        public void FreeManaged() {
            obj = null;
        }

        public void FreeUnmanaged() {
            UnsafeUtility.Free(ptr, Allocator);
            ptr = null;
        }
    }
}