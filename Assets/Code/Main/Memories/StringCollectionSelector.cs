using System;
using System.Linq;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Memories {
    /// <summary>
    /// Selector based on sorted string collection with implemented overrides for equality members.
    /// </summary>
    public struct StringCollectionSelector : IEquatable<StringCollectionSelector> {
        string[] _context; // Saved
        string _singleContext; // Saved
        int? _hashCode;

        public string[] ContextPure => _context; // Used in saving
        public string SingleContextPure => _singleContext; // Used in saving
        public static readonly StringCollectionSelector Empty = new StringCollectionSelector(string.Empty);

        public StringCollectionSelector(params string[] values) {
            if (values is { Length: > 0 }) {
                Array.Sort(values);
                ArrayUtils.SquashDuplicatesSorted(ref values);
                if (values.Length == 1) {
                    _singleContext = values[0];
                    _context = Array.Empty<string>();
                    _hashCode = null;
                    return;
                } 
                _context = values;
            } else {
                _context = Array.Empty<string>();
            }

            _singleContext = null;
            _hashCode = null;
        }

        public StringCollectionSelector(string values) {
            _singleContext = values;
            _context = Array.Empty<string>();
            _hashCode = null;
        }
        
        [UnityEngine.Scripting.Preserve]
        public bool Contains(string value) {
            return _singleContext != null ? _singleContext == value : _context.Contains(value);
        }
        public bool ContainsPartial(string value) {
            return _singleContext?.Contains(value) ?? _context.Any(v => v.Contains(value));
        }

        public bool Equals(StringCollectionSelector other) {
            return _singleContext == other._singleContext && ArrayUtils.Equals(_context, other._context);
        }

        public override bool Equals(object obj) {
            return obj is StringCollectionSelector a && Equals(a);
        }

        public override int GetHashCode() {
            return _hashCode ??= CalculateHashCode();
        }

        public override string ToString() {
            return _singleContext ?? string.Join('|', _context);
        }

        int CalculateHashCode() {
            int result = _singleContext?.GetHashCode() ?? 0;
            unchecked {
                foreach (string s in _context) {
                    result = result * 31 + s.GetHashCode();
                }
            }
            return result;
        }
    }
}