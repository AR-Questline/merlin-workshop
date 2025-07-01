using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Editor.Helpers.Tags {
    public class TagsCache {
        const string DataPath = "Data/Tags";
        static readonly Dictionary<TagsCategory, TagsCache> Cache = new();
        
        public readonly TagsCategory category;
        readonly string directory;
        
        public Entry[] entries;
        
        /// <summary> should content of directory be updated </summary>
        public bool dirty;
        
        TagsCache(TagsCategory category) {
            this.category = category;
            directory = $@"{Application.dataPath}\..\{DataPath}\{category}";
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
        }
        
        /// <summary>
        /// Use to reset the cache after external actions, if files have been modified outside of Unity (e.g. git revert).
        /// </summary>
        public static void ResetCache() {
            Cache.Clear();
        }
        
        public static TagsCache Get(TagsCategory category) {
            if (!Cache.TryGetValue(category, out var cache)) {
                cache = new TagsCache(category);
                cache.entries = ReadKindCaches(cache.directory);
                Cache.Add(category, cache);
            }
            return cache;
        }

        public static IEnumerable<TagsCache> LoadAll() {
            foreach (var category in Enum.GetValues(typeof(TagsCategory))) {
                yield return Get((TagsCategory)category);
            }
        }

        public static void SaveAll() {
            foreach (var cache in Cache.Values) {
                cache.Save();
            }
        }

        public bool TryFindEntryIndex(string kind, out int index) {
            for (int i = 0; i < entries.Length; i++) {
                if (entries[i].kind.value == kind) {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        public int AddEntry(string kind) {
            var index = entries.Length;
            var entry = new Entry() {
                kind = new StringWithContext(kind, null),
                values = Array.Empty<StringWithContext>()
            };
            ArrayUtils.Add(ref entries, entry);
            dirty = true;
            return index;
        }
        
        public bool TryFindEntry(string kind, out Entry entry) {
            for (int i = 0; i < entries.Length; i++) {
                if (entries[i].kind.value == kind) {
                    entry = entries[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }

        public bool TryRemoveKind(string kind) {
            if (TryFindEntryIndex(kind, out int index)) {
                ArrayUtils.RemoveAt(ref entries, index);
                dirty = true;
                return true;
            }
            return false;

        }
        
        public string GetContext(string tag) {
            TagUtils.Split(tag, out var kind, out var value);
            string kindContext = null;
            string valueContext = null;
            if (TryFindEntry(kind, out var entry)) {
                kindContext = entry.kind.context;
                if (entry.TryFindValue(value, out var stringWithContext)) {
                    valueContext = stringWithContext.context;
                }
            }
            return $"{kind}: {kindContext}\n{value}: {valueContext}";
        }
        
        public string GetKindContext(string kind) {
            return TryFindEntry(kind, out var entry) ? entry.kind.context : null;
        }
        
        public string GetValueContext(string kind, string value) {
            return TryFindEntry(kind, out var entry) && entry.TryFindValue(value, out var stringWithContext) ? stringWithContext.context : null;
        }

        public void Save() {
            for (int i = 0; i < entries.Length; i++) {
                ref var kind = ref entries[i];
                if (!kind.dirty) {
                    continue;
                }
                if (kind.values.Length == 0) {
                    ArrayUtils.RemoveAt(ref entries, i);
                    i--;
                    dirty = true;
                    continue;
                }
                var path = $@"{directory}\{kind.kind.value}.tag";
                using var file = new FileStream(path, FileMode.Create);
                using var writer = new StreamWriter(file);
                writer.WriteLine(kind.kind.context);
                for (int j = 0; j < kind.values.Length; j++) {
                    writer.WriteLine($"{kind.values[j].value}\t{kind.values[j].context}");
                }
                kind.dirty = false;
            }
            if (dirty) {
                foreach (string file in Directory.GetFiles(directory)) {
                    if (!file.EndsWith(".tag")) {
                        continue;
                    }
                    var name = Path.GetFileNameWithoutExtension(file);
                    if (entries.All(entry => entry.kind.value != name)) {
                        File.Delete(file);
                    }
                }
                dirty = false;
            }
        }
        
        static Entry[] ReadKindCaches(string directory) {
            var files = Directory.GetFiles(directory);
            var entries = new List<Entry>(files.Length);
            var reusableStringWithContexts = new List<StringWithContext>();
            for (int i = 0; i < files.Length; i++) {
                if (!files[i].EndsWith(".tag")) {
                    continue;
                }
                var name = Path.GetFileNameWithoutExtension(files[i]);
                var content = File.ReadAllLines(files[i]);
                for (int j = 1; j < content.Length; j++) {
                    var parts = content[j].Split('\t');
                    if (parts.Length == 2) {
                        reusableStringWithContexts.Add(new StringWithContext(parts[0], parts[1]));
                    }
                }
                var entry = new Entry {
                    kind = new StringWithContext(name, content[0]),
                    values = reusableStringWithContexts.ToArray()
                };
                entries.Add(entry);
                reusableStringWithContexts.Clear();
            }
            return entries.ToArray();
        }
        
        public struct Entry {
            public StringWithContext kind;
            public StringWithContext[] values;
            
            /// <summary> should content of file be updated </summary>
            public bool dirty;
            
            public bool TryFindValueIndex(string value, out int index) {
                for (int i = 0; i < values.Length; i++) {
                    if (values[i].value == value) {
                        index = i;
                        return true;
                    }
                }
                index = -1;
                return false;
            }

            public int AddValue(string value) {
                int index = values.Length;
                ArrayUtils.Add(ref values, new StringWithContext(value, ""));
                dirty = true;
                return index;
            }
            
            public bool TryFindValue(string value, out StringWithContext stringWithContext) {
                for (int i = 0; i < values.Length; i++) {
                    if (values[i].value == value) {
                        stringWithContext = values[i];
                        return true;
                    }
                }
                stringWithContext = default;
                return false;
            }
        }
        
        public struct StringWithContext {
            public string value;
            public string context;

            public StringWithContext(string value, string context) {
                this.value = value;
                this.context = context;
            }
        }
    }
}