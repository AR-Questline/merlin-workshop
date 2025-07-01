using System;
using System.IO;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.Utility.LowLevel {
    public readonly unsafe struct FileWriter : IDisposable {
        readonly FileStream _stream;

        public uint Position => (uint)_stream.Position;

        public FileWriter(string filepath, FileMode mode = FileMode.Create) {
            _stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        public void Write<T>(T value) where T : unmanaged {
            var size = sizeof(T);
            var bytePtr = (byte*)&value;
            var buffer = new ReadOnlySpan<byte>(bytePtr, size);
            _stream.Write(buffer);
        }

        public void Write<T>(UnsafeArray<T>.Span span) where T : unmanaged {
            var buffer = new ReadOnlySpan<byte>(span.Ptr, (int)(span.Length * sizeof(T)));
            _stream.Write(buffer);
        }

        public void Write<T>(T[] array) where T : unmanaged {
            fixed (T* arrayPtr = array) {
                var buffer = new ReadOnlySpan<byte>(arrayPtr, array.Length * sizeof(T));
                _stream.Write(buffer);
            }
        }

        public void Write<T>(T* buffer, int length) where T : unmanaged {
            _stream.Write(new ReadOnlySpan<byte>(buffer, length * sizeof(T)));
        }

        public void Dispose() {
            _stream.Dispose();
        }
    }
}