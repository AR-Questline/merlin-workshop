using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Awaken.TG.MVC.Serialization {
    public unsafe struct BinaryWriter {
        public const byte Escape = 0x5C;
        public const byte SpecialStart = 0x7B;
        public const byte SpecialSeparator = 0x7C;
        public const byte SpecialEnd = 0x7D;

        const int BufferSize = 64;

        readonly Stream _stream;
        readonly byte[] _buffer;
        int _bufferHead;
        
        public BinaryWriter(Stream stream) {
            _stream = stream;
            _buffer = new byte[BufferSize];
            _bufferHead = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte b) {
            if ((b == SpecialEnd) | (b == SpecialSeparator) | (b == SpecialStart) | (b == Escape)) {
                WriteByte(Escape);
            }
            WriteByte(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteStart() {
            WriteByte(SpecialStart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSeparator() {
            WriteByte(SpecialSeparator);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteEnd() {
            WriteByte(SpecialEnd);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte* ptr, int length) {
            for (int i = 0; i < length; i++) {
                Write(ptr[i]);
            }
        }

        void WriteByte(byte b) {
            _buffer[_bufferHead++] = b;
            if (_bufferHead == BufferSize) {
                _stream.Write(_buffer, 0, BufferSize);
                _bufferHead = 0;
            }
        }

        public void Flush() {
            if (_bufferHead != 0) {
                _stream.Write(_buffer, 0, _bufferHead);
            }
            _stream.Flush();
        }
    }
}