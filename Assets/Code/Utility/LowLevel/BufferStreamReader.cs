using System;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.Utility.LowLevel {
    public unsafe struct BufferStreamReader {
        UnsafeArray<byte> _data;
        uint _position;

        public BufferStreamReader(UnsafeArray<byte> data) {
            _data = data;
            _position = 0;
        }

        public T Read<T>() where T : unmanaged {
            if (!TryRead(out T value)) {
                throw new InvalidOperationException($"Failed to read value of type {typeof(T)}, not enough data. Position: {_position}, Data Length: {_data.Length}");
            }
            return value;
        }

        public bool TryRead<T>(out T value) where T : unmanaged {
            var size = (uint)sizeof(T);
            if (_position + size > _data.Length) {
                value = default;
                return false;
            }
            var currentPtr = _data.Ptr + _position;
            value = *(T*)currentPtr;
            _position += size;
            return true;
        }

        public UnsafeArray<T>.Span ReadSpan<T>(uint length) where T : unmanaged {
            if (!TryReadSpan(length, out UnsafeArray<T>.Span value)) {
                throw new InvalidOperationException($"Failed to read span of type {typeof(T)}, not enough data. Position: {_position}, Data Length: {_data.Length}, Requested Length: {length}");
            }
            return value;
        }

        public bool TryReadSpan<T>(uint length, out UnsafeArray<T>.Span value) where T : unmanaged {
            var size = length * (uint)sizeof(T);
            if (_position + size > _data.Length) {
                value = default;
                return false;
            }
            var currentPtr = _data.Ptr + _position;
            value = UnsafeArray<T>.FromExistingData((T*)currentPtr, length);
            _position += size;
            return true;
        }
        
        public UnsafeArray<T>.Span ReadRest<T>() where T : unmanaged {
            var length = (_data.Length - _position) / (uint)sizeof(T);
            return ReadSpan<T>(length);
        }
    }
}
