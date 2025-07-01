using System.Collections.Generic;

namespace Awaken.TG.Main.Utility.RichLabels {
    public static class RichLabelUtilities {
        // Checks if the given RichLabelSet matches given RichLabelUsageEntries.
        public static bool IsMatchingRichLabel(RichLabelSet richLabelSet, IEnumerable<RichLabelUsageEntry> richLabelUsageEntries) {
            foreach (var entry in richLabelUsageEntries) {
                bool isLabelIncludedInSet = richLabelSet.Contains(entry.RichLabelGuid);
                if (isLabelIncludedInSet) {
                    if (!entry.Include) {
                        return false;
                    }
                } else {
                    if (!entry.Include) {
                        continue;
                    }

                    return false;
                }
            }
            return true;
        }
    }
}
