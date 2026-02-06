using System.IO;
using System.IO.Compression;
using JetBrains.Annotations;

namespace Awaken.TG.MVC.Serialization {
    public class ARDeflateStream : DeflateStream, IARStream {
        const int BufferSize = 64;
        readonly byte[] _buffer;
        int _bufferHead;

        public ARDeflateStream([NotNull] Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, false) { }

        public ARDeflateStream([NotNull] Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : base(
            stream, compressionLevel, leaveOpen) {
            _buffer = new byte[BufferSize];
        }

        public ARDeflateStream([NotNull] Stream stream, CompressionMode mode) : this(stream, mode, false) { }

        public ARDeflateStream([NotNull] Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode,
            leaveOpen) {
            _buffer = new byte[BufferSize];
        }

        public void AboutToWrite(int byteCount) {
            // Cannot do anything better :(
        }

        public override void WriteByte(byte value) {
            _buffer[_bufferHead++] = value;
            if (_bufferHead == BufferSize) {
                Write(_buffer, 0, BufferSize);
                _bufferHead = 0;
            }
        }

        public override void Flush() {
            if (_bufferHead > 0) {
                Write(_buffer, 0, _bufferHead);
                _bufferHead = 0;
            }
            base.Flush();
        }
    }
}