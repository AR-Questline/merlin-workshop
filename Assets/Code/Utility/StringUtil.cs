using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Awaken.Utility {
    public static class StringUtil {
        [UnityEngine.Scripting.Preserve]
        public static string TrimStart(this string source, string trimValue) {
            return source.StartsWith(trimValue) ? source[trimValue.Length..] : source;
        }
        
        public static string NicifyName(string name) {
            int index = 0;
            Span<char> buffer = stackalloc char[name.Length * 2];

            bool firstLetterPushed = false;
            var previousCharType = CharType.WhiteSpace;
            foreach (var c in name) {
                var currentCharType = GetCharType(c);

                if (currentCharType is CharType.Unknown) {
                    Debugging.Log.Important?.Warning($"Unknown character type in name to Nicify ({name})");
                    continue;
                }

                if (firstLetterPushed && ShouldInsertSpace(previousCharType, currentCharType)) {
                    buffer[index++] = ' ';
                }

                if (currentCharType != CharType.WhiteSpace) {
                    buffer[index++] = !firstLetterPushed && currentCharType is CharType.LowerCaseLetter
                        ? char.ToUpperInvariant(c)
                        : c;
                    firstLetterPushed = true;
                }

                previousCharType = currentCharType;
            }

            return buffer[..index].ToString();
        }
        
        public static string NicifyTypeName(object target) => target == null ? null : NicifyName(target.GetType().Name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ShouldInsertSpace(CharType previousCharType, CharType currentCharType) {
            return (previousCharType, currentCharType) is
                (CharType.WhiteSpace, not CharType.WhiteSpace) or
                (CharType.Digit, CharType.LowerCaseLetter or CharType.UpperCaseLetter) or
                (CharType.LowerCaseLetter or CharType.UpperCaseLetter, CharType.Digit) or
                (CharType.LowerCaseLetter, CharType.UpperCaseLetter);
        }

        static CharType GetCharType(char c) {
            if (char.IsWhiteSpace(c) || c is '_') {
                return CharType.WhiteSpace;
            }

            if (char.IsLetter(c)) {
                if (char.IsUpper(c)) {
                    return CharType.UpperCaseLetter;
                }
                if (char.IsLower(c)) {
                    return CharType.LowerCaseLetter;
                }
                return CharType.Unknown;
            }
            if (char.IsDigit(c)) {
                return CharType.Digit;
            }
            return CharType.Unknown;
        }
        
        enum CharType : byte {
            Unknown,
            WhiteSpace,
            UpperCaseLetter,
            LowerCaseLetter,
            Digit,
        }
    }
}