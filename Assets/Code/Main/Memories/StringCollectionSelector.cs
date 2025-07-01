using System;
using System.Linq;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Memories {
    /// <summary>
    /// Selector based on sorted string collection with implemented overrides for equality members.
    /// </summary>
    public partial class StringCollectionSelector {
        public ushort TypeForSerialization => SavedTypes.StringCollectionSelector;

        [Saved] string[] _context;
        int? _hashCode;

        public static readonly StringCollectionSelector Empty = new StringCollectionSelector();
        
         [JsonConstructor, UnityEngine.Scripting.Preserve] StringCollectionSelector() {

            _context = Array.Empty<string>();
        }
        
        public StringCollectionSelector(params string[] values) {
            if (values is { Length: > 0 }) {
                Array.Sort(values);
                ArrayUtils.SquashDuplicatesSorted(ref values);
                _context = values;
            } else {
                _context = Array.Empty<string>();
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public bool Contains(string value) => _context.Contains(value);
        public bool ContainsPartial(string value) => _context.Any(v => v.Contains(value));

        public bool Equals(StringCollectionSelector other) {
            return other != null && ArrayUtils.Equals(_context, other._context);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StringCollectionSelector a && Equals(a);
        }

        public override int GetHashCode() {
            return _hashCode ??= CalculateHashCode();
        }

        public override string ToString() {
            return string.Join('|', _context);
        }

        int CalculateHashCode() {
            int result = 0;
            unchecked {
                foreach (string s in _context) {
                    result = result * 31 + s.GetHashCode();
                }
            }
            return result;
        }
    }
}