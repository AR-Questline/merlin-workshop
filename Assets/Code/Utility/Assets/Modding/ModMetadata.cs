using System;
using System.IO;
using Awaken.Utility.Collections;

namespace Awaken.Utility.Assets.Modding {
    public struct ModMetadata {
        public string name;
        public string version;
        public string author;
        public string[] tags;

        public static ModMetadata Load(string name) {
            var modName = name;
            var modVersion = "1.0";
            var modAuthor = "unknown";
            var modTags = new StructList<string>(0);
            
            var file = $"{ModManager.ModDirectoryPath}/{name}/mod.meta";
            if (File.Exists(file)) {
                foreach (var line in File.ReadLines(file)) {
                    var span = line.AsSpan();
                    var colon = span.IndexOf(':');
                    if  (colon < 0) {
                        continue;
                    }
                    
                    var key = span[..colon].Trim();
                    var value = span[(colon + 1)..].Trim();
                    
                    if (key.Equals("name", StringComparison.OrdinalIgnoreCase)) {
                        modName = value.ToString();
                    } else if (key.Equals("version", StringComparison.OrdinalIgnoreCase)) {
                        modVersion = value.ToString();
                    } else if (key.Equals("author", StringComparison.OrdinalIgnoreCase)) {
                        modAuthor = value.ToString();
                    } else if (key.Equals("tags", StringComparison.OrdinalIgnoreCase)) {
                        int start = 0;
                        for (int i = 0; i <= value.Length; i++) {
                            if (i == value.Length || value[i] == ' ') {
                                var tag = value.Slice(start, i - start).Trim();
                                if (tag.Length > 0) {
                                    modTags.Add(tag.ToString());
                                }
                                start = i + 1;
                            }
                        }
                    }
                }
            }
            
            return new ModMetadata {
                name = modName,
                version = modVersion,
                author = modAuthor,
                tags = modTags.ToArray(),
            };
        }
    }
}