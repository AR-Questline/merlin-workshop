using System.Runtime.CompilerServices;

namespace Awaken.TG.MVC.Serialization {
    public unsafe struct BinaryWriter {
        public const byte Escape = 0x5C;
        public const byte SpecialStart = 0x7B;
        public const byte SpecialSeparator = 0x7C;
        public const byte SpecialEnd = 0x7D;

        readonly IARStream _stream;
        
        public BinaryWriter(IARStream stream) {
            _stream = stream;
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
            _stream.AboutToWrite(length);
            for (int i = 0; i < length; i++) {
                Write(ptr[i]);
            }
        }

        void WriteByte(byte b) {
            _stream.WriteByte(b);
        }

        public void Flush() {
            _stream.Flush();
        }
    }
}