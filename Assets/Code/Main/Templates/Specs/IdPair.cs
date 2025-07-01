using System;

namespace Awaken.TG.Main.Templates.Specs {
    [Serializable]
    public struct IdPair : IEquatable<IdPair>, IComparable<IdPair> {
        public string fullPath;
        public SpecId id;

        public IdPair(string fullPath, in SpecId id) {
            this.fullPath = fullPath;
            this.id = id;
        }

        // === Equality overrides
        public bool Equals(IdPair other) {
            return id == other.id && fullPath == other.fullPath;
        }
        
        public override bool Equals(object obj) {
            return obj is IdPair other && Equals(other);
        }
        
        public override int GetHashCode() {
            unchecked {
                int hashCode = (fullPath != null ? fullPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ id.GetHashCode();
                return hashCode;
            }
        }

        // === Operator overrides
        public static bool operator ==(IdPair pair1, IdPair pair2) {
            return pair1.Equals(pair2);
        }

        public static bool operator !=(IdPair pair1, IdPair pair2) {
            return !pair1.Equals(pair2);
        }

        public int CompareTo(IdPair other) {
            int idComparison = id.CompareTo(other.id);
            if (idComparison != 0) return idComparison;
            return string.Compare(fullPath, other.fullPath, StringComparison.Ordinal);
        }
    }
}