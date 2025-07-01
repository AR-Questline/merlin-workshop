using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Editor.Localizations {
    public static class StoryScriptRedundancyRemoval {
        public static List<ScriptEntry> RemoveRedundantEntries(List<ScriptEntry> entries, VOFilterType filterType) {
            List<ScriptSequence> sequencesInOrder = new();
            ScriptSequence currentSequence = new();

            foreach (ScriptEntry entry in entries) {
                if (entry.IsSeparator) {
                    currentSequence.RemoveLastChoice();
                    if (!currentSequence.IsEmpty) {
                        sequencesInOrder.Add(currentSequence);
                        currentSequence = new();
                    }
                } else {
                    currentSequence.entries.Add(entry);
                }
            }

            // Remove all duplicates
            HashSet<ScriptSequence> sequencesSet = new();
            sequencesInOrder.RemoveAll(s => !sequencesSet.Add(s));

            // Remove all sequences that are contained within other sequence
            List<ScriptSequence> toRemove = new();
            foreach (var seq in sequencesInOrder) {
                if (sequencesInOrder.Any(s => !ReferenceEquals(s, seq) && s.IsSuperSetOf(seq))) {
                    toRemove.Add(seq);
                }
            }

            foreach (var s in toRemove) {
                sequencesInOrder.Remove(s);
            }

            // Remove entries based on the filter
            if (filterType != VOFilterType.All) {
                foreach (var sequence in sequencesInOrder) {
                    sequence.entries.RemoveAll(e => !MatchFilter(e, filterType));
                }

                sequencesInOrder.RemoveAll(s => s.entries.All(e => !e.IsText));
            }

            List<ScriptEntry> finalList = new();
            foreach (var seq in sequencesInOrder) {
                finalList.AddRange(seq.entries);
                finalList.Add(ScriptEntry.Separator);
            }

            return finalList;
        }

        static bool MatchFilter(ScriptEntry entry, VOFilterType filterType) {
            if (filterType == VOFilterType.All || !entry.IsText) {
                return true;
            }

            LocTermData termData = LocalizationTools.TryGetTermData(entry.id);
            var audioMeta = termData.englishEntry?.GetMetadata<AudioReplacementName>();
            string audioReplacementName = audioMeta?.AudioReplacement;
            bool audioMissing = string.IsNullOrWhiteSpace(audioReplacementName);
            if (audioMissing) {
                return filterType.HasFlagFast(VOFilterType.Missing);
            }

            // Check if the VO was made with the same text
            bool mismatch = termData.englishEntry.LocalizedValue.GetHashCode() != audioMeta.TranslationHash;
            if (mismatch) {
                return filterType.HasFlagFast(VOFilterType.Mismatched);
            }

            return filterType.HasFlagFast(VOFilterType.HasVO);
        }

        public class ScriptSequence {
            public readonly List<ScriptEntry> entries = new();

            public bool IsEmpty => !entries.Any();

            public bool IsSuperSetOf(ScriptSequence other) {
                int indexThere = 0;
                int indexHere = entries.IndexOf(other.entries[indexThere]);
                int indexOffset = indexHere;

                if (indexHere == -1) return false;

                while (indexHere - indexThere == indexOffset && indexThere < other.entries.Count - 1) {
                    indexThere++;
                    indexHere = entries.IndexOf(other.entries[indexThere]);
                }

                return indexHere - indexThere == indexOffset;
            }

            public void RemoveLastChoice() {
                int lastIndex = entries.Count - 1;
                if (lastIndex >= 0 && entries[lastIndex].IsChoice) {
                    entries.RemoveAt(lastIndex);
                }
            }

            public bool Equals(ScriptSequence other) {
                return entries.SequenceEqual(other.entries);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return Equals((ScriptSequence)obj);
            }

            public override int GetHashCode() {
                return (entries != null ? entries.GetSequenceHashCode() : 0);
            }
        }
    }
}