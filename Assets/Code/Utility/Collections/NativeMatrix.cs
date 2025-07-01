using System;
using Unity.Collections;

namespace Awaken.Utility.Collections {
    public struct NativeMatrix<T> : IDisposable where T : struct {
        NativeArray<T> _matrix;
        
        public int Width { [UnityEngine.Scripting.Preserve] get; }
        public int Height { get; }
        
        public NativeMatrix(int width, int height, Allocator allocator, NativeArrayOptions options) {
            Width = width;
            Height = height;
            _matrix = new NativeArray<T>(width * height, allocator, options);
        }

        public T this[int x, int y] {
            get => _matrix[x * Height + y];
            set => _matrix[x * Height + y] = value;
        }

        public void Dispose() {
            _matrix.Dispose();
        }
    }
}