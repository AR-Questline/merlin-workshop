// for sourcegen

using System;
using System.IO;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.MVC.Serialization {
    public unsafe partial class SaveWriter : IDisposable {
        BinaryWriter _writer;
        readonly SaveWriterContext _context;

        public SaveWriter(Stream stream, in SaveWriterContext context) {
            _writer = new BinaryWriter(stream);
            _context = context;
        }

        public void Dispose() {
            _writer.Flush();
        }

        public void WriteByte(byte b) {
            _writer.Write(b);
        }
        
        public void Write<T>(T value) where T : unmanaged {
            _writer.Write((byte*)&value, sizeof(T));
        }

        public void Write(string value) {
            if (string.IsNullOrEmpty(value)) {
                _writer.Write(0);
                _writer.Write(0);
                return;
            }
            fixed (char* ptr = value) {
                _writer.Write((byte*)ptr, value.Length * sizeof(char));
                _writer.Write(0);
                _writer.Write(0);
            }
        }
        
        public void WriteAscii(string value) {
            if (string.IsNullOrEmpty(value)) {
                _writer.Write(0);
                return;
            }
            int length = value.Length;
            fixed (char* ptr = value) {
                for (int i = 0; i < length; i++) {
                    _writer.Write((byte)ptr[i]);
                }
                _writer.Write(0);
            }
        }

        public void WriteAsciiNoEnd(string value) {
            if (string.IsNullOrEmpty(value)) {
                return;
            }
            int length = value.Length;
            fixed (char* ptr = value) {
                for (int i = 0; i < length; i++) {
                    _writer.Write((byte)ptr[i]);
                }
            }
        }

        public void WriteType(ushort type) { Write(type); }
        public void WriteName(ushort nameId) { Write(nameId); }
        public void WriteStart() { _writer.WriteStart(); }
        public void WriteSeparator() { _writer.WriteSeparator(); }
        public void WriteEnd() { _writer.WriteEnd(); }
    }
}