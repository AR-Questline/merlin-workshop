using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Awaken.PackageUtilities.Collections {
    public static unsafe class NativeCollectionPackageExt {
        public static CollectionComparableView<T> ComparableView<T>(in this ARUnsafeList<T> list) where T : unmanaged, IEquatable<T> {
            return new CollectionComparableView<T>(list.Ptr, list.Length);
        }
        
        public static CollectionComparableView<T> ComparableView<T>(in this NativeArray<T> list) where T : unmanaged, IEquatable<T> {
            return new CollectionComparableView<T>((T*)list.GetUnsafePtr(), list.Length);
        }
        
        public static bool Contains<T, U>(in this ARUnsafeList<T> list, in U value) where T : unmanaged where U : IEquatable<T> {
            return Contains(list.Ptr, list.Length, value);
        }
        
        public static bool Contains<T, U>(in this NativeArray<T> list, in U value) where T : unmanaged where U : IEquatable<T> {
            return Contains((T*)list.GetUnsafePtr(), list.Length, value);
        }
        
        public static bool Contains<T, U>(in this CollectionComparableView<T> list, in U value) where T : unmanaged, IEquatable<T> where U : IEquatable<T> {
            return Contains(list.Ptr, list.Length, value);
        }
        
        public static bool Contains<T, U>(T* ptr, int length, in U value) where T : unmanaged where U : IEquatable<T> {
            for (int i = 0; i < length; i++) {
                if (ptr[i].Equals(value)) {
                    return true;
                }
            }
            return false;
        }
    }
}