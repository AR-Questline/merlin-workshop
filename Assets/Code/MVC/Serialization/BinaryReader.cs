using System;
using System.IO;

namespace Awaken.TG.MVC.Serialization {
    public readonly unsafe struct BinaryReader {
        readonly Stream _stream;
        
        public BinaryReader(Stream stream) {
            _stream = stream;
        }

        public ReadResult TryRead(out byte result) {
            var read = _stream.ReadByte();
            if (read == -1) {
                result = 0;
                return ReadResult.EndOfStream;
            }
            result = (byte)read;
            if (result == BinaryWriter.SpecialStart) {
                return ReadResult.SpecialStart;
            }
            if (result == BinaryWriter.SpecialSeparator) {
                return ReadResult.SpecialSeparator;
            }
            if (result == BinaryWriter.SpecialEnd) {
                return ReadResult.SpecialEnd;
            }
            if (result == BinaryWriter.Escape) {
                read = _stream.ReadByte();
                if (read == -1) {
                    throw new Exception("Invalid byte sequence");
                }
                result = (byte)read;
            }
            return ReadResult.Normal;
        }

        public void Read(out byte result) {
            if (TryRead(out result) != ReadResult.Normal) {
                throw new Exception("Invalid byte sequence");
            }
        }
        
        public void Read(byte* ptr, int length) {
            for (int i = 0; i < length; i++) {
                var result = TryRead(out ptr[i]);
                if (result != ReadResult.Normal) {
                    throw new Exception("Invalid byte sequence");
                }
            }
        }

        public void Dispose() {
            _stream.Flush();
        }
        
        public enum ReadResult : byte {
            Normal,
            EndOfStream,
            SpecialStart,
            SpecialSeparator,
            SpecialEnd,
        }
    }
}