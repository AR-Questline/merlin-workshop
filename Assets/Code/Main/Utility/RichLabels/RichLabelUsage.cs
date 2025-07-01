using System;
using System.Linq;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Utility.RichLabels {
    [Serializable]
    public sealed class RichLabelUsage {
        [SerializeField] RichLabelUsageEntry[] richLabelUsageEntries = Array.Empty<RichLabelUsageEntry>();
        [SerializeField] RichLabelConfigType richLabelConfigType;
        public RichLabelUsageEntry[] RichLabelUsageEntries => richLabelUsageEntries;
        
#if UNITY_EDITOR
        public static string Editor_RichLabelUsageEntriesPropertyName => nameof(richLabelUsageEntries);
        public static string Editor_RichLabelConfigTypePropertyName => nameof(richLabelConfigType);
        public RichLabelConfigType Editor_ConfigType => richLabelConfigType;
#endif

        public RichLabelUsage(RichLabelConfigType richLabelConfigType) {
            this.richLabelConfigType = richLabelConfigType;
        }
        
        public RichLabelUsage(RichLabelUsage richLabelUsage) {
            richLabelUsageEntries = new RichLabelUsageEntry[richLabelUsage.richLabelUsageEntries.Length];
            richLabelUsage.richLabelUsageEntries.CopyTo(richLabelUsageEntries, 0);
            richLabelConfigType = richLabelUsage.richLabelConfigType;
        }
        
        public bool Equals(RichLabelUsage other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (richLabelConfigType != other.richLabelConfigType) return false;
            if (richLabelUsageEntries.Length != other.richLabelUsageEntries.Length) return false;
            
            for (int i = 0; i < other.richLabelUsageEntries.Length; i++) {
                if (!Contains(other.richLabelUsageEntries[i])) return false;
            }
            return true;
        }
        
        public bool Contains(string richLabelGuid) {
            for (int i = 0; i < richLabelUsageEntries.Length; i++) {
                if (richLabelUsageEntries[i].RichLabelGuid == richLabelGuid) {
                    return true;
                }
            }
            return false;
        }
        
        public bool Contains(RichLabelUsageEntry entry) {
            for (int i = 0; i < richLabelUsageEntries.Length; i++) {
                if (richLabelUsageEntries[i].RichLabelGuid == entry.RichLabelGuid) {
                    return richLabelUsageEntries[i].Include == entry.Include;
                }
            }
            return false;
        }

        public bool Matches(RichLabelUsage toCheck) {
            if (toCheck.richLabelConfigType != richLabelConfigType) return false;
            for (int i = 0; i < toCheck.richLabelUsageEntries.Length; i++) {
                if (Contains(toCheck.richLabelUsageEntries[i].RichLabelGuid) != toCheck.richLabelUsageEntries[i].Include) return false;
            }
            return true;
        }

        public override string ToString() {
            return $"RichLabelUsage: {richLabelConfigType.ToStringFast()} - {richLabelUsageEntries.Length} entries\n {string.Join("\n", richLabelUsageEntries.Select(entry => entry.RichLabelGuid + " - " + entry.Include))}";
        }

        public static SerializationAccessor Serialization(RichLabelUsage instance) => new(instance);
        
        public struct SerializationAccessor {
            readonly RichLabelUsage _instance;
            
            public SerializationAccessor(RichLabelUsage instance) {
                _instance = instance;
            }
            
            public ref RichLabelUsageEntry[] RichLabelUsageEntries => ref _instance.richLabelUsageEntries;
            public ref RichLabelConfigType RichLabelConfigType => ref _instance.richLabelConfigType;
        }
    }

    [Serializable]
    public partial class RichLabelUsageEntry {
        public ushort TypeForSerialization => SavedTypes.RichLabelUsageEntry;

        [SerializeField, Saved] string richLabelGuid;
        [SerializeField, Saved] bool include;

        public string RichLabelGuid => richLabelGuid;
        
#if UNITY_EDITOR
        public static string Editor_GuidPropertyName => nameof(richLabelGuid);
        public static string Editor_IncludePropertyName => nameof(include);
#endif

        public bool Include {
            get => include;
            set => include = value;
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public RichLabelUsageEntry() {}
        
        public RichLabelUsageEntry(string richLabelGuid, bool include) {
            this.richLabelGuid = richLabelGuid;
            this.include = include;
        }
        
        public bool Equals(RichLabelUsageEntry other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return richLabelGuid == other.richLabelGuid && include == other.include;
        }
        
        public static SerializationAccessor Serialization(RichLabelUsageEntry instance) => new(instance);
        
        public struct SerializationAccessor {
            readonly RichLabelUsageEntry _instance;
            
            public SerializationAccessor(RichLabelUsageEntry instance) {
                _instance = instance;
            }
            
            public ref string RichLabelGuid => ref _instance.richLabelGuid;
            public ref bool Include => ref _instance.include;
        }
    }
}