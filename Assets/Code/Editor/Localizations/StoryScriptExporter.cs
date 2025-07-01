using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Localizations {
    public static class StoryScriptExporter {
        static Type[] s_startingTypes = { typeof(StoryStartEditorNode), typeof(SEditorBookmark) };

        static readonly HashSet<string> KnownSequences = new();
        static readonly HashSet<StoryNode> NodesHistory = new();
        static readonly List<ScriptEntry> TempList = new(40);
        static readonly HashSet<string> DeduplicatedSequences = new();

        public static void ExportAllGraphs(ScriptType type, StoryScriptExporterWindow.Settings settings) {
            List<ScriptEntry> entries = new();
            List<StoryGraph> graphs;
            if (settings.SelectBy == StoryScriptExporterWindow.Settings.SelectionType.Directory) {
                graphs = TemplatesSearcher.FindAllOfType<StoryGraph>()
                    .Where(g => settings.directoryPaths.Any(p => AssetDatabase.GetAssetPath(g).Contains($"{p}/")))
                    .ToList();
            } else {
                graphs = settings.storyGraphs;
            }

            NodesHistory.Clear();

            for (int i = 0; i < graphs.Count; i++) {
                var graph = graphs[i];
                EditorUtility.DisplayProgressBar("Progress", $"{graph.name} {i}/{graphs.Count}", (float)i / graphs.Count);
                List<ScriptEntry> graphEntries = EnumerateStoryGraph(graph, type).ToList();
                if (graphEntries.Any()) {
                    entries.Add(new ScriptEntry("", graph.name));
                    entries.Add(ScriptEntry.Separator);
                    entries.AddRange(graphEntries);
                    entries.Add(ScriptEntry.Separator);
                }
            }

            EditorUtility.ClearProgressBar();

            if (type is ScriptType.VoiceActors) {
                entries = StoryScriptRedundancyRemoval.RemoveRedundantEntries(entries, settings.voFilter);
            }

            if (settings.actorFilter != DefinedActor.None.ActorRef) {
                string actorName = ActorsRegister.Get.Editor_GetActorName(settings.actorFilter);
                entries = entries.Where(e => Regex.IsMatch(e.actor, actorName)).ToList();
            }

            if (settings.exportOnlyRedacted) {
                entries = entries.Where(IsRedacted).ToList();
            }

            if (settings.forceExclusionOfDuplicates) {
                entries = entries.GroupBy(x => (x.text, x.actor)).Select(x => x.First()).ToList();
            }

            if (settings.separatePerActor) {
                string folderPath = EditorUtility.SaveFolderPanel("Choose Save Location", Application.dataPath, "Story Scripts by Actors");
                WriteToSeparateCSVs(settings, folderPath, entries);
            } else {
                string pathToSave = EditorUtility.SaveFilePanel("Choose Save Location", Application.dataPath, "story_script", "csv");
                WriteToCSV(entries, pathToSave, settings.exportOnlyTexts);
            }
        }

        static bool IsRedacted(ScriptEntry entry) {
            LocTermData termData = LocalizationTools.TryGetTermData(entry.id);
            int? storedHash = termData.englishEntry?.GetMetadata<TermStatusMeta>()?.TranslationHash;
            int currentHash = entry.text.Trim().GetHashCode();
            return storedHash == currentHash;
        }

        public static IEnumerable<ScriptEntry> EnumerateOnlyOneStoryGraph(StoryGraph graph, ScriptType type) {
            NodesHistory.Clear();
            return EnumerateStoryGraph(graph, type);
        }

        static IEnumerable<ScriptEntry> EnumerateStoryGraph(StoryGraph graph, ScriptType type) {
            KnownSequences.Clear();
            DeduplicatedSequences.Clear();

            List<StoryNode> startingNodes = graph.nodes
                .OfType<StoryNode>()
                .Where(IsOfStartingType)
                .OrderBy(n => n is StoryStartEditorNode ? 0 : 1)
                .ToList();

            foreach (var node in startingNodes) {
                foreach (var entry in ExploreNode(node, type)) {
                    if (!string.IsNullOrWhiteSpace(entry.text)) {
                        yield return entry;
                    }
                }
            }
        }

        static bool IsOfStartingType(StoryNode node) {
            return s_startingTypes.Any(type => type == node.GetType() || node.elements.Any(e => type == e.GetType()));
        }

        static IEnumerable<ScriptEntry> ExploreNode(StoryNode node, ScriptType type) {
            return type switch {
                ScriptType.Texts => ExploreNodeForTexts(node),
                ScriptType.Dialogues => ExploreNodeForDialogs(node),
                ScriptType.VoiceActors => ExploreNodeForVoiceActors(node),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        static IEnumerable<ScriptEntry> ExploreNodeForDialogs(StoryNode node, List<ScriptEntry> history = null, HashSet<StoryNode> nodesHistory = null) {
            history ??= new List<ScriptEntry>();
            nodesHistory ??= new HashSet<StoryNode>();
            if (node == null || !nodesHistory.Add(node)) {
                foreach (ScriptEntry scriptEntry in ReturnHistory(history)) {
                    yield return scriptEntry;
                }

                yield break;
            }

            bool hasAnyContinuation = false;
            var elements = node.elements;
            foreach (var element in elements) {
                ElementExtractor extractor = ElementExtractor.GetFor(element.GetType());
                List<ScriptEntry> historyForThis = new();
                foreach (var entry in extractor.GetTexts(element)) {
                    historyForThis.Add(entry);
                }

                bool hasContinuation = false;
                bool hasNullContinuation = false;
                foreach (var continuation in extractor.GetContinuations(element, ScriptType.Dialogues)) {
                    hasNullContinuation = true;
                    if (continuation != null) {
                        List<ScriptEntry> fullHistory = new(history);
                        fullHistory.AddRange(historyForThis);
                        foreach (var entry in ExploreNodeForDialogs(continuation, fullHistory, new HashSet<StoryNode>(nodesHistory))) {
                            hasAnyContinuation = true;
                            hasContinuation = true;
                            yield return entry;
                        }
                    }
                }

                if (!hasContinuation) {
                    history.AddRange(historyForThis);
                    if (historyForThis.Any() && hasNullContinuation) {
                        foreach (ScriptEntry scriptEntry in ReturnHistory(history)) {
                            yield return scriptEntry;
                        }
                    }
                }
            }

            if (node is IEditorChapter { ContinuationChapter: StoryNode nextNode }) {
                foreach (var entry in ExploreNodeForDialogs(nextNode, new List<ScriptEntry>(history), new HashSet<StoryNode>(nodesHistory))) {
                    yield return entry;
                }
            } else if (!hasAnyContinuation) {
                foreach (ScriptEntry scriptEntry in ReturnHistory(history)) {
                    yield return scriptEntry;
                }
            }
        }

        static IEnumerable<ScriptEntry> ReturnHistory(List<ScriptEntry> history) {
            string hash = string.Join("", history.Select(h => h.GetHashCode()));
            if (KnownSequences.Add(hash)) {
                foreach (var entry in history) {
                    yield return entry;
                }

                if (history.Any()) {
                    yield return ScriptEntry.Separator;
                }
            }
        }

        static IEnumerable<ScriptEntry> ExploreNodeForVoiceActors(StoryNode node, List<ScriptEntry> history = null, HashSet<StoryNode> nodesHistory = null) {
            history ??= new List<ScriptEntry>();
            nodesHistory ??= new HashSet<StoryNode>();
            if (node == null || !nodesHistory.Add(node)) {
                foreach (ScriptEntry scriptEntry in ReturnHistoryForVO(history)) {
                    yield return scriptEntry;
                }

                yield break;
            }

            bool hasAnyContinuation = false;
            var elements = node.elements;
            foreach (var element in elements) {
                bool isChoice = false;
                ElementExtractor extractor = ElementExtractor.GetFor(element.GetType());
                List<ScriptEntry> historyForThis = new();
                foreach (var entry in extractor.GetTexts(element)) {
                    historyForThis.Add(entry);

                    if (entry.IsPartOfChoice) {
                        isChoice = true;
                    }
                }

                bool hasContinuation = false;
                bool hasNullContinuation = false;
                foreach (var continuation in extractor.GetContinuations(element, ScriptType.VoiceActors)) {
                    hasNullContinuation = true;
                    if (continuation != null) {
                        List<ScriptEntry> fullHistory = new();
                        if (!isChoice) {
                            fullHistory.AddRange(history);
                        }

                        fullHistory.AddRange(historyForThis);

                        if (isChoice) {
                            List<ScriptEntry> tempList = history.Concat(historyForThis).ToList();
                            foreach (ScriptEntry scriptEntry in ReturnHistoryForVO(tempList)) {
                                yield return scriptEntry;
                            }
                        }

                        foreach (var entry in ExploreNodeForVoiceActors(continuation, fullHistory, new HashSet<StoryNode>(nodesHistory))) {
                            hasAnyContinuation = true;
                            hasContinuation = true;
                            yield return entry;
                        }
                    }
                }

                if (!hasContinuation) {
                    history.AddRange(historyForThis);
                    if (historyForThis.Any() && hasNullContinuation) {
                        foreach (ScriptEntry scriptEntry in ReturnHistoryForVO(history)) {
                            yield return scriptEntry;
                        }
                    }
                }
            }

            if (node is IEditorChapter { ContinuationChapter: StoryNode nextNode }) {
                foreach (var entry in ExploreNodeForVoiceActors(nextNode, new List<ScriptEntry>(history), new HashSet<StoryNode>(nodesHistory))) {
                    yield return entry;
                }
            } else if (!hasAnyContinuation) {
                foreach (ScriptEntry scriptEntry in ReturnHistoryForVO(history)) {
                    yield return scriptEntry;
                }
            }
        }
        
        static IEnumerable<ScriptEntry> ReturnHistoryForVO(List<ScriptEntry> history) {
            TempList.Clear();
            TempList.AddRange(history.Where(h => !h.IsPartOfChoice));
            string deduplicatedHash = string.Join("", TempList.Select(h => h.GetHashCode()));
            bool cutoff = !DeduplicatedSequences.Add(deduplicatedHash);

            string hash = string.Join("", history.Select(h => h.GetHashCode()));
            if (KnownSequences.Add(hash)) {
                bool hasAny = false;
                foreach (var entry in history) {
                    if (cutoff) {
                        yield return new ScriptEntry("", "");
                    } else {
                        hasAny = true;
                        yield return entry;
                    }
                }

                if (hasAny) {
                    yield return ScriptEntry.Separator;
                }
            }
        }

        static IEnumerable<ScriptEntry> ExploreNodeForTexts(StoryNode node) {
            foreach (var element in StoryExplorerUtil.ExploreNode(node, NodesHistory)) {
                ElementExtractor extractor = ElementExtractor.GetFor(element.GetType());
                foreach (var entry in extractor.GetTexts(element)) {
                    entry.previousLine = StoryExplorerUtil.GetPreviousLine(element);
                    yield return entry;
                }
            }
        }

        static void WriteToSeparateCSVs(StoryScriptExporterWindow.Settings settings, string folderPath, List<ScriptEntry> entries) {
            var actors = entries.Select(s => s.actor).Distinct().Where(a => !string.IsNullOrWhiteSpace(a) && a != ScriptEntry.SeparatorCellValue).ToList();
            Parallel.For(0, actors.Count, (i, _) => {
                var actor = actors[i];
                string path = folderPath + $"/{actor}.csv";
                var actorEntries = entries.Where(e => e.actor == actor);
                WriteToCSV(actorEntries.ToList(), path, settings.exportOnlyTexts);
            });
        }

        static void WriteToCSV(List<ScriptEntry> entries, string path, bool exportOnlyTexts) {
            File.Delete(path);
            using var stream = File.OpenWrite(path);
            using var writer = new StreamWriter(stream);
            if (!exportOnlyTexts) {
                // using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                // csv.Context.RegisterClassMap<DataToExportMap>();
                // csv.WriteRecords(entries);
            } else {
                var textEntries = entries.Select(a => a.text);
                foreach (var textEntry in textEntries) {
                    writer.WriteLine($"\"{textEntry}\"");
                }
            }
        }
    }

    public class ScriptEntry : IEquatable<ScriptEntry> {
        public static readonly string SeparatorCellValue = "------------------------------";

        public readonly string id;
        public readonly string type; // Choice, Choice Footer, Text
        public readonly string actor;
        public readonly string text;
        public string previousLine;

        public static ScriptEntry Separator => new(SeparatorCellValue,
            SeparatorCellValue,
            SeparatorCellValue,
            SeparatorCellValue);

        public ScriptEntry(string id, string text, string type = "", string actor = "") {
            this.id = id;
            this.text = text;
            this.type = type;
            this.actor = actor;
        }

        public bool IsSeparator => id.Contains("------");
        public bool IsText => type == "Text";
        public bool IsChoice => type == "Choice";
        public bool IsPartOfChoice => IsChoice || IsChoiceFooter;
        bool IsChoiceFooter => type == "Choice Footer";

        public string AsPreviousLine => $"({actor}) {text}";

        public bool Equals(ScriptEntry other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id && type == other.type && actor == other.actor && text == other.text;
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ScriptEntry)obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (id != null ? id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (type != null ? type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (actor != null ? actor.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (text != null ? text.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    // [UsedImplicitly]
    // sealed class DataToExportMap : ClassMap<ScriptEntry> {
    //     public DataToExportMap() {
    //         Map(m => m.id).Index(0).Name("ID");
    //         Map(m => m.type).Index(1).Name("Type");
    //         Map(m => m.actor).Index(2).Name("Actor");
    //         Map(m => m.text).Index(3).Name("Text");
    //         Map(m => m.previousLine).Index(4).Name("Previous Line");
    //     }
    // }
}