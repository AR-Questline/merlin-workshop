using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.Utility.Collections;
using JetBrains.Annotations;

namespace Awaken.Utility.Extensions {
    public static class StringExtensions {
        public static string Capitalize(this string s) {
            if (s.Length == 0) return s;
            return s.Substring(0, 1).ToUpperInvariant() + s.Substring(1);
        }

        [UnityEngine.Scripting.Preserve]
        public static string Uncapitalize(this string s) {
            if (s.Length == 0) return s;
            return s.Substring(0, 1).ToLowerInvariant() + s.Substring(1);
        }

        public static string ToIdentifier(this string s) {
            return Regex.Replace(s, @"\s+", "_").ToLower();
        }

        public static int CountCharacter(this string s, char character) {
            var count = 0;
            foreach (char stringPart in s) {
                if (stringPart == character) {
                    ++count;
                }
            }
            return count;
        }
        
        public static bool ContainsAny(this string pattern, ICollection<string> enumerable, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase, bool valueWhenEmpty = true) {
            return enumerable.IsEmpty() ? valueWhenEmpty : enumerable.Any(v => pattern.Contains(v, comparison));
        }
        
        [ContractAnnotation("null => true")]
        public static bool IsNullOrWhitespace(this string s) => string.IsNullOrWhiteSpace(s);
    }
}
