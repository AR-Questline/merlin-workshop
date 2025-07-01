using System.Linq;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Utility.Tags {
    public static class TierHelper {
        public static readonly TierTags ItemTiers = new() {
            tier0 = "item:tier0",
            tier1 = "item:tier1",
            tier2 = "item:tier2",
            tier3 = "item:tier3",
            tier4 = "item:tier4",
            tier5 = "item:tier5",
            tier6 = "item:tier6",
            tier7 = "item:tier7",
        };
        
        public static Tier GetTier(string[] tags, in TierTags tierTags) {
            if (tags.Contains(tierTags.tier7)) {
                return Tier.Tier7;
            }
            if (tags.Contains(tierTags.tier6)) {
                return Tier.Tier6;
            }
            if (tags.Contains(tierTags.tier5)) {
                return Tier.Tier5;
            }
            if (tags.Contains(tierTags.tier4)) {
                return Tier.Tier4;
            }
            if (tags.Contains(tierTags.tier3)) {
                return Tier.Tier3;
            }
            if (tags.Contains(tierTags.tier2)) {
                return Tier.Tier2;
            }
            if (tags.Contains(tierTags.tier1)) {
                return Tier.Tier1;
            }
            if (tags.Contains(tierTags.tier0)) {
                return Tier.Tier0;
            }
            return Tier.None;
        }
            
        public static void SetTier(ref string[] tags, Tier tier, in TierTags tierTags) {
            ArrayUtils.Remove(ref tags, tierTags.tier0);
            ArrayUtils.Remove(ref tags, tierTags.tier1);
            ArrayUtils.Remove(ref tags, tierTags.tier2);
            ArrayUtils.Remove(ref tags, tierTags.tier3);
            ArrayUtils.Remove(ref tags, tierTags.tier4);
            ArrayUtils.Remove(ref tags, tierTags.tier5);
            ArrayUtils.Remove(ref tags, tierTags.tier6);
            ArrayUtils.Remove(ref tags, tierTags.tier7);
                
            var tag = tier switch {
                Tier.Tier0 => tierTags.tier0,
                Tier.Tier1 => tierTags.tier1,
                Tier.Tier2 => tierTags.tier2,
                Tier.Tier3 => tierTags.tier3,
                Tier.Tier4 => tierTags.tier4,
                Tier.Tier5 => tierTags.tier5,
                Tier.Tier6 => tierTags.tier6,
                Tier.Tier7 => tierTags.tier7,
                _ => null
            };
            if (tag != null) {
                ArrayUtils.Add(ref tags, tag);
            }
        }

        public enum Tier : byte {
            None,
            Tier0,
            Tier1,
            Tier2,
            Tier3,
            Tier4,
            Tier5,
            Tier6,
            Tier7,
        }
            
        public struct TierTags {
            public string tier0;
            public string tier1;
            public string tier2;
            public string tier3;
            public string tier4;
            public string tier5;
            public string tier6;
            public string tier7;
        }
    }
}