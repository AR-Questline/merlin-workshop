using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Awaken.PackageUtilities.Collections {
    public static unsafe class UnsafeSort {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<TData>(in ARUnsafeList<TData> list) where TData : unmanaged, IComparable<TData> {
            NativeSortExtension.Sort(list.Ptr, list.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<TData, TComparer>(in ARUnsafeList<TData> list, in TComparer comparer) where TData : unmanaged where TComparer : IComparer<TData> {
            NativeSortExtension.Sort(list.Ptr, list.Length, comparer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<TData>(TData* array, int length) where TData : unmanaged, IComparable<TData> {
            NativeSortExtension.Sort(array, length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<TData, TComparer>(TData* array, int length, in TComparer comparer) where TData : unmanaged where TComparer : IComparer<TData> {
            NativeSortExtension.Sort(array, length, comparer);
        }
    }
}