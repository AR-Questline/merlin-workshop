using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.Utility.Extensions;

namespace Awaken.Utility {
    public readonly struct SearchPattern : IEquatable<SearchPattern> {
        public static SearchPattern Empty => new(String.Empty);
        static readonly Regex SearchRegex = new("\"(.+)\"|\\s|\\.|;", RegexOptions.Compiled);

        readonly int _hashCode;
        readonly string[] _searchParts;

        public bool IsEmpty => _searchParts.Length == 0;
        public string[] SearchParts => _searchParts;

        public SearchPattern(string searchContext) {
            _hashCode = searchContext?.GetHashCode() ?? 0;
            if (string.IsNullOrWhiteSpace(searchContext)) {
                _searchParts = Array.Empty<string>();
            } else {
                _searchParts = SearchRegex.Split(searchContext)
                    .Where(static s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
            }
        }

        public SearchPattern Update(string searchContext) {
            var newHash = searchContext?.GetHashCode() ?? 0;
            return newHash == _hashCode ? this : new SearchPattern(searchContext);
        }

        public bool HasSearchInterest(string content) {
            return content.ContainsAny(_searchParts);
        }

        public bool HasExactSearch(string content) {
            return _searchParts.Any(s => content.Equals(s, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool HasSearchInterest(string[] contents) {
            foreach (var content in contents) {
                if (content.ContainsAny(_searchParts)) {
                    return true;
                }
            }
            return false;
        }

        [UnityEngine.Scripting.Preserve]
        public bool HasSearchInterest(List<string> contents) {
            foreach (var content in contents) {
                if (content.ContainsAny(_searchParts)) {
                    return true;
                }
            }
            return false;
        }

        [UnityEngine.Scripting.Preserve]
        public bool HasSearchInterest(HashSet<string> contents) {
            foreach (var content in contents) {
                if (content.ContainsAny(_searchParts)) {
                    return true;
                }
            }
            return false;
        }

        [UnityEngine.Scripting.Preserve]
        public bool HasSearchInterest(IEnumerable<string> contents) {
            foreach (var content in contents) {
                if (content.ContainsAny(_searchParts)) {
                    return true;
                }
            }
            return false;
        }

        public bool Equals(SearchPattern other) {
            return _hashCode == other._hashCode;
        }

        public override bool Equals(object obj) {
            return obj is SearchPattern other && Equals(other);
        }

        public override int GetHashCode() {
            return _hashCode;
        }

        public static bool operator ==(SearchPattern left, SearchPattern right) {
            return left.Equals(right);
        }

        public static bool operator !=(SearchPattern left, SearchPattern right) {
            return !left.Equals(right);
        }
    }
}
