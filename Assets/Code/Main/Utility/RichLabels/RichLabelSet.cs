using System;
using System.Collections.Generic;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Utility.RichLabels {
    [Serializable]
    public partial class RichLabelSet {
        public ushort TypeForSerialization => SavedTypes.RichLabelSet;

        [Saved] public string ownerGuid;
        [Saved] public List<string> richLabelGuids;

        public RichLabelConfigType configType;
        
        public ARGuid OwnerGuid {
            get {
                if (ownerGuid.IsNullOrWhitespace()) {
                    ownerGuid = Guid.NewGuid().ToString();
                }
                return new(ownerGuid);
            }
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        RichLabelSet() {}

        public RichLabelSet(RichLabelConfigType configType) {
            ownerGuid = Guid.NewGuid().ToString();
            richLabelGuids = new List<string>();
            this.configType = configType;
        }
        
        public RichLabelSet(RichLabelSet other) {
            ownerGuid = other.ownerGuid;
            richLabelGuids = new List<string>(other.richLabelGuids);
            configType = other.configType;
        }
        
        public void AssignNewGuid() {
            ownerGuid = Guid.NewGuid().ToString();
        }
        
        public bool Contains(string guid) {
            return richLabelGuids.Contains(guid);
        }

        public bool Equals(RichLabelSet set) {
            return ownerGuid == set.ownerGuid && EqualRichLabelGuids(set);
        }
        
        public bool EqualRichLabelGuids(RichLabelSet set) {
            return richLabelGuids.Count == set.richLabelGuids.Count && richLabelGuids.TrueForAll(set.Contains);
        }
    }
}