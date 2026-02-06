using System.IO;
using Awaken.Utility.Debugging;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Awaken.TG.MVC.Serialization {
    public class NativeMemoryStream : Stream, IARStream {
        UnsafeList<byte> _buffer;
        int _position;

        public override bool CanRead => _buffer.IsCreated;
        public override bool CanSeek => _buffer.IsCreated;
        public override bool CanWrite => _buffer.IsCreated;
        public override long Length => _buffer.Length;

        public override long Position {
            get => _position;
            set => _position = (int)value;
        }

        public NativeMemoryStream(Allocator allocator) : this(1200, allocator) { }

        public NativeMemoryStream(int initialCapacity, Allocator allocator) {
            _buffer = new UnsafeList<byte>(initialCapacity, allocator);
        }

        public override void Flush() { }

        public override unsafe int Read(byte[] buffer, int offset, int count) {
            fixed (byte* ptr = buffer) {
                var dataPtr = ptr + offset;
                var bytesToRead = math.min(count, _buffer.Length - _position);
                UnsafeUtility.MemCpy(dataPtr, _buffer.Ptr + _position, bytesToRead);
                _position += bytesToRead;
                return bytesToRead;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) {
            if (origin == SeekOrigin.Begin) {
                _position = (int)offset;
            } else if (origin == SeekOrigin.Current) {
                _position += (int)offset;
            } else if (origin == SeekOrigin.End) {
                _position = _buffer.Length + (int)offset;
            } else {
                Log.Important?.Error("Invalid SeekOrigin: " + origin);
            }

            return _position;
        }

        public override unsafe void SetLength(long value) {
            var requiredLength = (int)value;
            var elementsToZero = requiredLength - _buffer.Length;
            _buffer.Length = requiredLength;
            if (elementsToZero > 0) {
                var startPtr = _buffer.Ptr + (_buffer.Length - elementsToZero);
                UnsafeUtility.MemSet(startPtr, 0, elementsToZero);
            }
        }

        public override unsafe void Write(byte[] buffer, int offset, int count) {
            fixed (byte* ptr = buffer) {
                var dataPtr = ptr + offset;
                _buffer.AddRange(dataPtr, count);
            }
        }

        public void AboutToWrite(int byteCount) {
            var requiredCapacity = _position + byteCount;
            if (requiredCapacity > _buffer.Capacity) {
                _buffer.SetCapacity(requiredCapacity);
            }
        }

        public override void WriteByte(byte value) {
            _buffer.Add(value);
        }

        protected override void Dispose(bool disposing) {
            if (!disposing) {
                return;
            }
            if (!_buffer.IsCreated) {
                Log.Important?.Error("Trying to dispose NativeMemoryStream that is already disposed.");
                return;
            }

            _buffer.Dispose();
        }

        public byte[] ToArray() {
            var array = new byte[_buffer.Length];
            unsafe {
                fixed (byte* ptr = array) {
                    UnsafeUtility.MemCpy(ptr, _buffer.Ptr, _buffer.Length);
                }
            }
            return array;
        }
    }
}