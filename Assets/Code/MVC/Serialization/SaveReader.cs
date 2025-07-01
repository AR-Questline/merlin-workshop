using System;
using System.IO;
using Awaken.Utility.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Awaken.TG.MVC.Serialization {
    public unsafe partial class SaveReader : IDisposable {
        const int MaxStringSize = 8 * 1024;

        readonly BinaryReader _reader;
        readonly char* _string;

        readonly SaveReaderContext _context;
        
        public SaveReader(Stream stream, in SaveReaderContext context) {
            _reader = new BinaryReader(stream);
            _string = (char*)UnsafeUtility.Malloc(MaxStringSize, UnsafeUtility.AlignOf<char>(), ARAlloc.Persistent);
            _context = context;
        }

        public void Dispose() {
            UnsafeUtility.Free(_string, ARAlloc.Persistent);
            _reader.Dispose();
        }
        
        public void Read<T>(out T value) where T : unmanaged {
            T temp;
            _reader.Read((byte*)&temp, sizeof(T));
            value = temp;
        }
        
        public void Read(out string value) {
            int length = 0;
            while (true) {
                Read(out char c);
                if (c == 0) {
                    break;
                }
#if AR_DEBUG || UNITY_EDITOR
                if (length >= MaxStringSize) {
                    throw new Exception("String Overflow");
                }
#endif
                _string[length++] = c;
            }
            value = length == 0 ? string.Empty : new string(_string, 0, length);
        }

        public void ReadAscii(out string value) {
            int length = 0;
            while (true) {
                _reader.Read(out var b);
                if (b == 0) {
                    break;
                }
#if AR_DEBUG || UNITY_EDITOR
                if (length >= MaxStringSize) {
                    throw new Exception("String Overflow");
                }
#endif
                _string[length++] = (char)b;
            }
            value = length == 0 ? string.Empty : new string(_string, 0, length);
        }
        
        public void ReadType(out ushort type) {
            Read(out type);
        }

        public bool TryReadValidType(out ushort type) {
            ReadType(out type);
            return type != 0;
        }
        
        public bool TryReadName(out ushort name) {
            ushort temp;
            byte* tempPtr = (byte*)&temp;
            if (_reader.TryRead(out tempPtr[0]) == BinaryReader.ReadResult.SpecialEnd) {
                name = 0;
                return false;
            }
            if (_reader.TryRead(out tempPtr[1]) != BinaryReader.ReadResult.Normal) {
                throw new Exception("Invalid byte sequence");
            }
            name = temp;
            return true;
        }

        public void ReadStart() {
            if (_reader.TryRead(out _) != BinaryReader.ReadResult.SpecialStart) {
                throw new Exception("Invalid byte sequence");
            }
        }

        public void ReadToSeparator() {
            int indent = 0;
            while (true) {
                var result = _reader.TryRead(out var b);
                if (indent == 0 && result == BinaryReader.ReadResult.SpecialSeparator) {
                    return;
                }
                if (result == BinaryReader.ReadResult.SpecialStart) {
                    ++indent;
                }
                if (result == BinaryReader.ReadResult.SpecialEnd) {
                    --indent;
                    if (indent < 0) {
                        throw new Exception("Invalid byte sequence");
                    }
                }
                if (result == BinaryReader.ReadResult.EndOfStream) {
                    throw new Exception("Invalid byte sequence");
                }
            }
        }

        public void ReadToEnd() {
            int indent = 0;
            while (true) {
                var result = _reader.TryRead(out var b);
                if (result == BinaryReader.ReadResult.SpecialStart) {
                    ++indent;
                }
                if (result == BinaryReader.ReadResult.SpecialEnd) {
                    if (indent == 0) {
                        return;
                    }
                    --indent;
                }
                if (result == BinaryReader.ReadResult.EndOfStream) {
                    throw new Exception("Invalid byte sequence");
                }
            }
        }
    }
}