using System;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal {
    [Serializable]
    public partial class ARGuid : IEquatable<ARGuid> {
        public virtual ushort TypeForSerialization => SavedTypes.ARGuid;

        [Saved, SerializeField]
        protected SerializableGuid _guid;
        
        public SerializableGuid GUID => _guid;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public ARGuid() { }

        public ARGuid(Guid guid) {
            this._guid = new(guid);
        }
        
        public ARGuid(string guid) {
            this._guid = new(guid);
        }
        
#if UNITY_EDITOR
        public static bool EDITOR_GuidsVisible = true;
        
        [ShowInInspector, HideLabel]
        string EditorDraw => _guid.ToString();
        
        public void EDITOR_SetGuid(Guid newGuid) {
            _guid = new(newGuid);
        }
        public void EDITOR_SetGuid(string newGuid) {
            _guid = new(newGuid);
        }
#endif

        public bool Equals(ARGuid other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _guid.Equals(other._guid);
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ARGuid) obj);
        }

        public override int GetHashCode() {
            return _guid.GetHashCode();
        }

        public static bool operator ==(ARGuid left, ARGuid right) {
            return Equals(left, right);
        }

        public static bool operator !=(ARGuid left, ARGuid right) {
            return !Equals(left, right);
        }

        public static SerializationAccessor Serialization(ARGuid instance) => new(instance);

        public struct SerializationAccessor {
            readonly ARGuid _instance;
            
            public SerializationAccessor(ARGuid instance) {
                _instance = instance;
            }
            
            public ref SerializableGuid GUID => ref _instance._guid;
        }
    }
}