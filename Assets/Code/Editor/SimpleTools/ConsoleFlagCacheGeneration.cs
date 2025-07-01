using System.Collections.Generic;
using System.IO;
using Awaken.TG.Editor.Helpers.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;

namespace Awaken.TG.Editor.SimpleTools {
    public static class ConsoleFlagCacheGeneration {
        [UnityEditor.MenuItem("TG/Design/Regenerate Console Flag Cache")]
        public static void RegenerateFlagCache() {
            var entries = TagsCache.Get(TagsCategory.Flag).entries;
            var flags = new List<string>();
            foreach (var entry in entries) {
                foreach (TagsCache.StringWithContext stringWithContext in entry.values) {
                    flags.Add(entry.kind.value + ":" + stringWithContext.value);
                }
            }
            
            const string FileName = "FlagSuggestionCache.txt";
            string path = Path.Combine(Application.streamingAssetsPath, FileName);
            
            File.Delete(path);
            
            using (FileStream fs = new(path, FileMode.CreateNew, FileAccess.Write)) {
                using (StreamWriter writer = new(fs)) {
                    foreach (var flag in flags) {
                        writer.WriteLine(flag);
                    }
                }
            }
        }
    }
}
