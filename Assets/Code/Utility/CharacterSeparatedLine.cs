using System;

namespace Awaken.Utility {
    public readonly ref struct CharacterSeparatedLine {
        readonly ReadOnlySpan<char> _line;
        readonly char _separator;
            
        public CharacterSeparatedLine(string line, char separator) {
            _line = line.AsSpan();
            _separator = separator;
        }
            
        public Enumerator GetEnumerator() {
            return new Enumerator(_line, _separator);
        }
            
        public ref struct Enumerator {
            readonly ReadOnlySpan<char> _line;
            readonly char _separator;
            int _start;
            int _length;

            public Enumerator(ReadOnlySpan<char> line, char separator) {
                _line = line;
                _separator = separator;
                _start = -1;
                _length = 0;
            }

            public bool MoveNext() {
                _start = _start + _length + 1;
                if (_start >= _line.Length) {
                    return false;
                }
                int i = _start;
                while (i < _line.Length && _line[i] != _separator) {
                    i++;
                }
                _length = i - _start;
                return true;
            }
            
            public ReadOnlySpan<char> Current => _line.Slice(_start, _length);
        }
    }
}