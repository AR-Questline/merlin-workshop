using System;
using System.Text.RegularExpressions;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Templates.Specs {
    [Serializable, InlineProperty]
    public partial struct SpecId : IEquatable<SpecId>, IComparable<SpecId> {
        public ushort TypeForSerialization => SavedTypes.SpecId;

        [Saved, HideInInspector] public string sceneName;
        [Saved, HideInInspector] public ulong prefabId;
        [Saved, HideInInspector] public ulong objectId;
        [Saved, HideInInspector] public byte baked;

        string _fullId;
        [ShowInInspector, HideLabel]
        public string FullId { get {
            if (!IsValid) {
                _fullId = string.Empty;
            } else if (_fullId.IsNullOrWhitespace()) {
                _fullId = $"{sceneName}_{prefabId.ToString()}_{objectId.ToString()}_{baked}";
            }
            return _fullId;
        }}

        public readonly bool IsValid => objectId != 0 && !string.IsNullOrWhiteSpace(sceneName);
        
        public SpecId(string sceneName, ulong prefabId, ulong objectId, byte baked) {
            this.prefabId = prefabId;
            this.objectId = objectId;
            this.sceneName = sceneName;
            this.baked = baked;
            _fullId = null;
        }

        static readonly Regex FullIdRegex = new(@"(.+)_(\d+)_(\d+)_(\d+)", RegexOptions.Compiled);
        public static SpecId FromFullId(string fullId) {
            var match = FullIdRegex.Match(fullId);
            if (match.Success) {
                var groups = match.Groups;
                return new SpecId(
                    groups[1].Value,
                    ulong.Parse(groups[2].Value),
                    ulong.Parse(groups[3].Value),
                    byte.Parse(groups[4].Value)
                );
            } else {
                return default;
            }
        }
        
        public readonly bool Equals(SpecId other) {
            return prefabId == other.prefabId && objectId == other.objectId && sceneName == other.sceneName;
        }

        public override bool Equals(object obj) {
            return obj is SpecId other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode =  prefabId.GetHashCode();
                hashCode = (hashCode * 397) ^ objectId.GetHashCode();
                hashCode = (hashCode * 397) ^ (sceneName != null ? sceneName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(in SpecId left, in SpecId right) => left.Equals(right);
        public static bool operator !=(in SpecId left, in SpecId right) => !left.Equals(right);

        public int CompareTo(SpecId other) {
            int prefabIdComparison = prefabId.CompareTo(other.prefabId);
            if (prefabIdComparison != 0) return prefabIdComparison;
            int objectIdComparison = objectId.CompareTo(other.objectId);
            if (objectIdComparison != 0) return objectIdComparison;
            return string.Compare(sceneName, other.sceneName, StringComparison.Ordinal);
        }

        public override string ToString() {
            return FullId;
        }
    }
}