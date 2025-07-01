using System;

namespace Awaken.TG.Editor.Utility.Parsers {
    public struct IntParserNoAlloc {
        int _value;

        void Reset() {
            _value = 0;
        }

        bool Advance(char c) {
            if (c < '0' || c > '9') {
                return false;
            }

            _value *= 10;
            _value += c - '0';
            return true;
        }

        public int Parse(string text, int start, int end) {
            Reset();
            bool negative = false;
            if (text[start] == '-') {
                negative = true;
                start++;
            }

            for (int i = start; i < end; i++) {
                if (!Advance(text[i])) {
                    throw new Exception($"Invalid Symbol {text[i]} at {i}");
                }
            }

            return negative ? -_value : _value;
        }
    }
}