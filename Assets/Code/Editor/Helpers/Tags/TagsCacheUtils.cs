using System;
using System.Collections.Generic;
using System.Globalization;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using UnityEditor;

namespace Awaken.TG.Editor.Helpers.Tags {
    public static class TagsCacheUtils {
        static readonly HashSet<string> ReusableStringSet = new();
        
        [InitializeOnLoadMethod]
        static void Init() {
            TagsEditorProxy.SetDelegates(RemoveTag, RenameTagKind, RenameTagValue);
        }
        
        public static void RemoveTag(string tag, TagsCategory category) {
            var cache = TagsCache.Get(category);
            
            var tagKind = TagUtils.TagKind(tag);
            var kindIndex = Array.FindIndex(cache.entries, entry => entry.kind.value.Equals(tagKind));
            if (kindIndex == -1) {
                return;
            }
            ref var entry = ref cache.entries[kindIndex];

            var tagValue = TagUtils.TagValue(tag);
            var valueIndex = Array.FindIndex(entry.values, value => value.value.Equals(tagValue));
            if (valueIndex == -1) {
                return;
            }
            
            ArrayUtils.RemoveAt(ref entry.values, valueIndex);
            entry.dirty = true;
            cache.Save();
        }
        
        public static void RemoveTagKind(string kind, TagsCategory category) {
            var cache = TagsCache.Get(category);
            
            if (cache.TryRemoveKind(kind)) {
                cache.Save();
            }
        }
        
        public static void RenameTagKind(string tag, string newKind, TagsCategory category) {
            var cache = TagsCache.Get(category);
            
            var tagKind = TagUtils.TagKind(tag);
            var kindIndex = Array.FindIndex(cache.entries, entry => entry.kind.value.Equals(tagKind));
            if (kindIndex == -1) {
                return;
            }
            ref var entry = ref cache.entries[kindIndex];
            
            entry.kind.value = newKind;
            entry.dirty = true;
            cache.dirty = true;
            cache.Save();
        }
        
        public static void RenameTagValue(string tag, string newValue, TagsCategory category) {
            var cache = TagsCache.Get(category);
            
            var tagKind = TagUtils.TagKind(tag);
            var kindIndex = Array.FindIndex(cache.entries, entry => entry.kind.value.Equals(tagKind));
            if (kindIndex == -1) {
                return;
            }
            ref var entry = ref cache.entries[kindIndex];
            
            var tagValue = TagUtils.TagValue(tag);
            var valueIndex = Array.FindIndex(entry.values, value => value.value.Equals(tagValue));
            if (valueIndex == -1) {
                return;
            }
            ref var value = ref entry.values[valueIndex];
            
            value.value = newValue;
            entry.dirty = true;
            cache.Save();
        }

        public static void AddTag(string tagKind, string tagValue, TagsCategory category) {
            var cache = TagsCache.Get(category);
            
            bool dirty = false;
            if (!cache.TryFindEntryIndex(tagKind, out var iKind)) {
                iKind = cache.AddEntry(tagKind);
                dirty = true;
            }
            ref var entry = ref cache.entries[iKind];
            
            if (!entry.TryFindValueIndex(tagValue, out _)) {
                entry.AddValue(tagValue);
                dirty = true;
            }

            if (dirty) {
                cache.Save();
            }
        }

        public static IEnumerable<string> KindHints(TagsCache cache, List<string> usedTags, string partialKind) {
            ReusableStringSet.Clear();
            foreach (var tag in usedTags) {
                var kind = TagUtils.TagKind(tag);
                ReusableStringSet.Add(kind);
            }
            try {
                var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                foreach (var entry in cache.entries) {
                    if (ReusableStringSet.Contains(entry.kind.value)) {
                        continue;
                    }
                    var kind = entry.kind.value;
                    if (compareInfo.IndexOf(kind, partialKind, CompareOptions.IgnoreCase) >= 0) {
                        yield return kind;

                    }
                }
            } finally {
                ReusableStringSet.Clear();
            }
        }

        public static IEnumerable<string> ValueHints(TagsCache cache, List<string> usedTags, string kind, string partialValue) {
            ReusableStringSet.Clear();
            foreach (var tag in usedTags) {
                var value = TagUtils.TagValue(tag);
                ReusableStringSet.Add(value);
            }
            try {
                if (!cache.TryFindEntry(kind, out var entry)) {
                    yield break;
                }
                var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                foreach(var value in entry.values) {
                    if (ReusableStringSet.Contains(value.value)) {
                        continue;
                    }
                    if (compareInfo.IndexOf(value.value, partialValue, CompareOptions.IgnoreCase) >= 0) {
                        yield return value.value;
                    }
                }
            } finally {
                ReusableStringSet.Clear();
            }
        }
    }
}