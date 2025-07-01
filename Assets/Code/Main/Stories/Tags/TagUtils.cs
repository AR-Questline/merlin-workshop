using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Extensions;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Tags {
    /// <summary>
    /// Utils for operations on tags
    /// </summary>
    public static class TagUtils {
        static readonly Regex IconNameRegex = new Regex("(?<=[Ii]con:).*", RegexOptions.Compiled);

        public static string GetTagID(string tag) {
            return $"Tag/{TagUtils.TagKind(tag).Capitalize()}{TagUtils.TagValue(tag).Capitalize()}";
        }
        
        public static string FirstTagOfKind(ICollection<string> tags, string kind) {
            return tags.FirstOrDefault(t => t.StartsWith(kind + ":"));
        }

        public static bool IsValidTag(string tag) {
            return tag.Contains(":") && !string.IsNullOrWhiteSpace(TagValue(tag)) && !string.IsNullOrWhiteSpace(TagKind(tag));
        }

        public static string TagKind(string tag) {
            int index = tag.IndexOf(":", StringComparison.InvariantCultureIgnoreCase);
            return tag.Substring(0, index);
        }

        public static string TagValue(string tag) {
            int index = tag.IndexOf(":", StringComparison.InvariantCultureIgnoreCase);
            return tag.Substring(index + 1);
        }
        
        public static void Split(string tag, out string kind, out string value) {
            int index = tag.IndexOf(':', StringComparison.InvariantCultureIgnoreCase);
            kind = tag[..index];
            value = tag[(index + 1)..];
        }

        [UnityEngine.Scripting.Preserve]
        public static string TryFindTagValue(ICollection<string> tags, string kind) {
            string tag = FirstTagOfKind(tags, kind);
            return string.IsNullOrEmpty(tag) ? null : TagValue(tag);
        }
        
        public static int? TryFindTagValueAsInt(ICollection<string> tags, string kind) {
            string tag = FirstTagOfKind(tags, kind);
            if (string.IsNullOrEmpty(tag)) {
                return null;
            }
            
            return int.TryParse(TagValue(tag), out int value) ? value : null;
        }

        [UnityEngine.Scripting.Preserve]
        public static string[] SplitTags(string tagString) {
            return tagString.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool FulfillsRequirements(ITagged tagged, ICollection<string> requirements, RequirementsMode mode) {
            return FulfillsRequirements(tagged.Tags, requirements, mode);
        }

        public static bool FulfillsRequirements(ICollection<string> provided, ICollection<string> requirements, RequirementsMode mode) {
            if (mode == RequirementsMode.And) {
                return TagUtils.HasRequiredTagsWithChecks(provided, requirements);
            }else if (mode == RequirementsMode.Or) {
                return requirements.Any(t => TagUtils.HasRequiredTagWithChecks(provided, t));
            }
            return false;
        }

        public static bool HasRequiredTags(ITagged tagged, ICollection<string> requiredTags) =>
            HasRequiredTags(tagged.Tags, requiredTags);

        public static bool HasRequiredTags(ICollection<string> providedTags, ICollection<string> requiredTags) {
            return requiredTags == null || requiredTags.All(tag => providedTags.Contains(tag, StringComparer.InvariantCultureIgnoreCase));
        }

        public static bool DoesNotHaveForbiddenTags(ITagged tagged, ICollection<string> forbiddenTags) {
            return DoesNotHaveForbiddenTags(tagged.Tags, forbiddenTags);
        }
        
        public static bool DoesNotHaveForbiddenTags(ICollection<string> providedTags, ICollection<string> forbiddenTags) {
            return forbiddenTags == null || !forbiddenTags.Any(providedTags.Contains);
        }
        
        public static bool HasRequiredTagsWithChecks(ICollection<string> providedTags, ICollection<string> requiredTags) {
            var requiredKinds = requiredTags.Where(r => TagUtils.TagValue(r).Equals("any", StringComparison.InvariantCultureIgnoreCase)).ToArray();
            requiredTags = requiredTags.Except(requiredKinds).ToList();
            requiredKinds = requiredKinds.Select(TagUtils.TagKind).ToArray();

            var fulfillsKinds = TagUtils.HasRequiredKinds(providedTags, requiredKinds);
            return fulfillsKinds && TagUtils.HasRequiredTags(providedTags, requiredTags);
        }
        
        public static bool HasAnyTag(ICollection<string> providedTags, ICollection<string> requiredTags) {
            return requiredTags.Any(providedTags.Contains);
        }

        public static bool HasRequiredTag(ITagged tagged, string requiredTag) =>
            HasRequiredTag(tagged?.Tags, requiredTag);

        public static bool HasRequiredTag(ICollection<string> providedTags, string requiredTag) {
            return providedTags?.Contains(requiredTag, StringComparer.InvariantCultureIgnoreCase) ?? false;
        }

        public static bool HasRequiredTagWithChecks(ICollection<string> providedTags, string requiredTag) {
            if (TagUtils.TagValue(requiredTag).Equals("any", StringComparison.InvariantCultureIgnoreCase)) {
                return TagUtils.HasRequiredKind(providedTags, requiredTag);
            }
            return TagUtils.HasRequiredTag(providedTags, requiredTag);
        }

        public static bool HasRequiredKinds(IEnumerable<string> providedTags, IEnumerable<string> requiredKinds) {
            var kinds = providedTags.Select(TagKind);
            return requiredKinds.All(kinds.Contains);
        }
        
        public static bool HasRequiredKind(IEnumerable<string> providedTags, string requiredKind) {
            var kinds = providedTags.Select(TagKind);
            return kinds.Contains( requiredKind );
        }

        [UnityEngine.Scripting.Preserve]
        public static int CountMatchingTags(IEnumerable<string> first, IEnumerable<string> second) {
            return first.Count(second.Contains);
        }

        [UnityEngine.Scripting.Preserve]
        public static string GenerateTag(string kindText, string valueText) {
            return $"{kindText.ToIdentifier()}:{valueText.ToIdentifier()}";
        }
        
        public static string IconNameFromTag(this string text) {
            return IconNameRegex.Match(text).Value;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static string FirstCharToUpper(this string input) {
            if (string.IsNullOrEmpty(input)) {
                return input;
            }
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
        
        public static void EDITOR_SAFE_RemoveTag(string tag, TagsCategory category) {
            TagsEditorProxy.RemoveTag(tag, category);
        }

        public static void EDITOR_SAFE_RenameTagKind(string tagToRename, string newTagKind, TagsCategory category) {
            TagsEditorProxy.RenameTagKind(tagToRename, newTagKind, category);
        }
        
        public static void EDITOR_SAFE_RenameTagValue(string tagToRename, string newTagValue, TagsCategory category) {
            TagsEditorProxy.RenameTagValue(tagToRename, newTagValue, category);
        }
        
        public enum RequirementsMode {
            And = 0,
            Or = 1,
        }
    }
}
