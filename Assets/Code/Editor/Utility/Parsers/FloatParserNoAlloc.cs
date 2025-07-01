using System;

namespace Awaken.TG.Editor.Utility.Parsers {
    public struct FloatParserNoAlloc {
        float _value;
        float _fractionWeight;
        bool _dotMet;
        IntParserNoAlloc _exponentParser;

        void Reset() {
            _value = 0;
            _fractionWeight = 1;
            _dotMet = false;
            _exponentParser = new();
        }

        bool Advance(char c) {
            if (c == '.') {
                if (_dotMet) {
                    return false;
                }

                _dotMet = true;
            } else {
                if (c < '0' || c > '9') {
                    return false;
                }

                if (_dotMet) {
                    _fractionWeight *= 0.1F;
                    _value += (c - '0') * _fractionWeight;
                } else {
                    _value *= 10;
                    _value += c - '0';
                }
            }

            return true;
        }

        public float Parse(string text, int start, int end) {
            Reset();
            bool negative = false;
            if (text[start] == '-') {
                negative = true;
                start++;
            }

            for (int i = start; i < end; i++) {
                if (text[i] == 'e') {
                    int exponent = _exponentParser.Parse(text, i + 1, end);
                    if (exponent > 0) {
                        int multiplier = 1;
                        for (int j = 0; j < exponent; j++) {
                            multiplier *= 10;
                        }

                        _value *= multiplier;
                    } else if (exponent < 0) {
                        float multiplier = 1;
                        for (int j = 0; j > exponent; j--) {
                            multiplier *= 0.1F;
                        }

                        _value *= multiplier;
                    }

                    break;
                } else {
                    if (!Advance(text[i])) {
                        throw new Exception($"Invalid Symbol {text[i]} at {i}");
                    }
                }
            }

            return negative ? -_value : _value;
        }
    }
}