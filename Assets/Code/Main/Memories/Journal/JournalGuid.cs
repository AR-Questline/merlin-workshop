using Awaken.Utility;
using System;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Memories.Journal {
    [Serializable]
    public partial class JournalGuid : ARGuid, IEquatable<JournalGuid> {
        public override ushort TypeForSerialization => SavedTypes.JournalGuid;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public JournalGuid() { }

        public JournalGuid(string guid) : base(guid) { }

        public bool Equals(JournalGuid other) {
            if (ReferenceEquals(null, other)) return false;
            return _guid.Equals(other._guid);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JournalGuid) obj);
        }

        public override int GetHashCode() {
            return _guid.GetHashCode();
        }

        public static bool operator ==(JournalGuid left, JournalGuid right) {
            return Equals(left, right);
        }

        public static bool operator !=(JournalGuid left, JournalGuid right) {
            return !Equals(left, right);
        }

        public override string ToString() => _guid.ToString();
    }
}