using System;
using System.IO;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Awaken.Utility.Collections {
    public static class MemoryStreamExtensions {
        public static unsafe UnsafeArray<byte> ToNativeArray(this MemoryStream memoryStream, Allocator allocator)
        {
            if (memoryStream.TryGetBuffer(out ArraySegment<byte> seg))
            {
                var managedArray = seg.Array;
                if (managedArray == null) {
                    throw new NullReferenceException("Managed array from memory stream is null");
                }
                var offset  = seg.Offset;
                var length  = seg.Count;
                var unsafeArray = new UnsafeArray<byte>((uint)length, allocator, NativeArrayOptions.UninitializedMemory);
                fixed (byte* srcPtr = &managedArray[offset])
                {
                    UnsafeUtility.MemCpy(unsafeArray.Ptr, srcPtr, length);
                }
                return unsafeArray;
            }
            else {
                var prevPosition = memoryStream.Position;
                var length = memoryStream.Length;
                var unsafeArray = new UnsafeArray<byte>((uint)length, allocator, NativeArrayOptions.UninitializedMemory);
                memoryStream.Position = 0;
                int bytesRead = memoryStream.Read(unsafeArray.AsSpan());
                if (bytesRead != length) {
                    throw new Exception("Bytes read != lenght");
                }
                memoryStream.Position = prevPosition;
                return unsafeArray;
            }
        }
    }
}