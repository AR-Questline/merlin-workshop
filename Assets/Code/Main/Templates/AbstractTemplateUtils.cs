using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Templates {
    public static class AbstractTemplateUtils {
        static readonly HashSet<ITemplate> Templates = new();
        
        public static PooledList<ITemplate> AllAbstracts(this ITemplate template) {
            Templates.Clear();
            FillWithDistinctAbstracts(template, Templates);
            PooledList<ITemplate>.Get(out var list);
            list.value.EnsureCapacity(Templates.Count);
            foreach (var foundTemplate in Templates) {
                list.value.Add(foundTemplate);
            }
            Templates.Clear();
            return list;
        }
        
        public static PooledList<T> AllAbstractsOfType<T>(this ITemplate template) where T : class, ITemplate {
            Templates.Clear();
            FillWithDistinctAbstracts(template, Templates);
            PooledList<T>.Get(out var list);
            // Distinct abstracts count is commonly up to 4 elements, so pre-allocating for all is not much overhead
            list.value.EnsureCapacity(Templates.Count);
            foreach (var foundTemplate in Templates) {
                if (foundTemplate is T templateT) {
                    list.value.Add(templateT);
                }
            }
            Templates.Clear();
            return list;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static HashSet<T> AllAbstractsSet<T>(this ITemplate template) {
            Templates.Clear();
            FillWithDistinctAbstracts(template, Templates);
            var result = new HashSet<T>(Templates.OfType<T>());
            Templates.Clear();
            return result;
        }
        
        static void FillWithDistinctAbstracts(ITemplate template, HashSet<ITemplate> abstracts) {
            if (abstracts.Add(template)) {
                var directAbstracts = template.DirectAbstracts;
                int directAbstractsCount = directAbstracts.value.Count;
                for (int i = 0; i < directAbstractsCount; i++) {
                    var directAbstract = directAbstracts.value[i];
                    if (directAbstract == null) {
                        Log.Important?.Error($"Null Abstract Template attached to: {template}!");
                        continue;
                    }
                    FillWithDistinctAbstracts(directAbstract, abstracts);
                }
                directAbstracts.Release();
            }
        }
        
        public static PooledList<T> Abstracts<T>(this ITemplate template) where T : class, ITemplate {
            return template.AllAbstractsOfType<T>();
        }

        public static PooledList<IAttachmentSpec> AllAttachments(this ITemplate template) {
            List<ITemplate> allAbstracts = template.AllAbstracts();
            PooledList<IAttachmentSpec>.Get(out var allAttachments);
            int count = allAbstracts.Count;
            for (int i = 0; i < count; i++) {
                using var directAttachments = allAbstracts[i].DirectAttachments;
                allAttachments.value.AddRange(directAttachments.value);
            }
            PooledList<ITemplate>.Release(allAbstracts);
            return allAttachments;
        }

        [UnityEngine.Scripting.Preserve]
        public static PooledList<T> Attachments<T>(this ITemplate template) {
            List<IAttachmentSpec> allAttachments = template.AllAttachments();
            PooledList<T>.Get(out var results);
            foreach (var attachment in allAttachments) {
                if (attachment is T t) {
                    results.value.Add(t);
                }
            }
            PooledList<IAttachmentSpec>.Release(allAttachments);
            return results;
        }

        public static bool InheritsFrom<T>(this ITemplate template, T @abstract) where T : ITemplate {
            if(template == null) {
                Log.Important?.Error($"Template is null");
                return false;
            }

            if (@abstract == null) {
                Log.Important?.Error($"Abstract template is null");
                return false;
            }
            Templates.Clear();
            FillWithDistinctAbstracts(template, Templates);
            bool inherits = Templates.Contains(@abstract);
            Templates.Clear();
            return inherits;
        }

        public static IEnumerable<string> AbstractTags(this ITemplate template) {
            var directAbstracts = template.DirectAbstracts;
            foreach (var directAbstract in directAbstracts.value) {
                if (directAbstract is ITagged tagged) {
                    foreach (var tag in tagged.Tags) {
                        yield return tag;
                    }
                }
            }
            directAbstracts.Release();
        }
        public static List<string> WithAbstractTags(this ITemplate template, IEnumerable<string> directTags) {
            return directTags.Concat(template.AbstractTags()).Distinct().ToList();
        }
    }
}