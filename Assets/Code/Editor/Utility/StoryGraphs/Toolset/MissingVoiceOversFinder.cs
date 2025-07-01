using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Utility.Audio;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using FMODUnity;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all graphs containing missing voice overs.")]
    public class MissingVoiceOversFinder : StoryGraphUtilityTool<SearchResult<MissingVOEntry>, MissingVOEntry> {
        [SerializeField, PropertyOrder(InputSectionOrder), LabelText("Show: ")]
        [InfoBox(@"Choose how to filter the results:
            - GraphDuplicates: Allow multiple entries per graph. Disable to skip repeated graphs.
            - ActorDuplicates: Allow multiple entries per actor. Disable to avoid duplicates from the same actor.
            - Barks: Include graphs marked as 'Bark'.
            - Debug: Include debug or obsolete graphs.
            - MissingClips: Include entries missing audio clips.
            - MissingEvents: Include entries missing FMOD event references.")]
        FilterMode filterMode = FilterMode.DefaultView;

        [SerializeField, PropertyOrder(InputSectionOrder), LabelText("Sort by: ")]
        SortMode sortMode = SortMode.Graph;
        
        [SerializeField, PropertyOrder(InputSectionOrder)]
        bool showMissingEventsAndClipsOnly = true;
        
        [BoxGroup(ResultSectionName, centerLabel :true), PropertyOrder(ResultSectionOrder)]
        [Button, EnableIf(nameof(ShouldEnableButtons))]
        public void GenerateVoiceOversCSV() {
            var graphs = ResultController.GatherResults().Select(p => (StoryGraph)p.TargetGraph);
            var filePaths = EditorAudioUtils.GetAllAudioFilePathsFromGraphs(graphs);
            FMODAudioToEventsExporter.ExportEventsToCSV(filePaths);
        }
        
        [BoxGroup(ResultSectionName, centerLabel :true), PropertyOrder(ResultSectionOrder)]
        [Button, EnableIf(nameof(ShouldEnableButtons))]
        public void UpdateVoiceOvers() {
            var graphs = ResultController.GatherResults().Select(p => (StoryGraph)p.TargetGraph);
            GraphConverterUtils.UpdateVoiceOvers(true, graphs.ToArray());
        }
        
        protected override bool Validate() => true;

        protected override void ExecuteTool() {
            var allTextElements = AllElements<StoryNode, SEditorText>();
            HashSet<NodeGraph> checkedGraphs = new();
            HashSet<ActorRef> checkedActors = new();
            ActorsRegister actorsRegister = ActorsRegister.Get;

            List<MissingVOEntry> results = new();

            foreach (var pair in allTextElements) {
                var sText = pair.element;
                var dialogueLine = sText.text.Translate();
                var graph = pair.graph as StoryGraph;

                if (graph == null) {
                    continue;
                }

                if (!sText.hasVoice || string.IsNullOrEmpty(dialogueLine)) {
                    continue;
                }

                var graphPath = AssetDatabase.GetAssetPath(graph);
                if (!filterMode.HasFlagFast(FilterMode.Debug) && IsDebugOrForRemoval(graph, graphPath)) {
                    continue;
                }

                if (!filterMode.HasFlagFast(FilterMode.Barks) && IsBark(graphPath)) {
                    continue;
                }
                
                if (!filterMode.HasFlagFast(FilterMode.GraphDuplicates) && checkedGraphs.Contains(graph)) {
                    continue;
                }

                if (!filterMode.HasFlagFast(FilterMode.ActorDuplicates) && checkedActors.Contains(sText.actorRef)) {
                    continue;
                }
                
                bool isMissingEvent = HasMissingEvent(sText, out string audioEventPath);
                bool isMissingClip = HasMissingClip(sText, out string audioClipName);
                
                // Skip if both event and clip are present
                if (!isMissingEvent && !isMissingClip) {
                    continue;
                }
                
                bool includeEvent = filterMode.HasFlagFast(FilterMode.MissingEvents) && isMissingEvent;
                bool includeClip = filterMode.HasFlagFast(FilterMode.MissingClips) && isMissingClip;
                
                if (!includeEvent && !includeClip) {
                    continue;
                }
                
                if(showMissingEventsAndClipsOnly && (!isMissingEvent || !isMissingClip)) {
                    continue;
                }
                
                var actorName = actorsRegister.Editor_GetActorName(sText.actorRef);
                results.Add(new MissingVOEntry(actorName, dialogueLine, audioEventPath, audioClipName, graph, pair.node));
                checkedGraphs.Add(graph);
                checkedActors.Add(sText.actorRef);
            }
            
            switch (sortMode) {
                case SortMode.Actor:
                    results.Sort((a, b) => string.Compare(a.ActorName, b.ActorName, StringComparison.Ordinal));
                    break;
                case SortMode.Graph:
                    results.Sort((a, b) => string.Compare(a.TargetGraph.name, b.TargetGraph.name, StringComparison.Ordinal));
                    break;
            }

            ResultController.Feed(results);
        }

        // === Odin
        bool ShouldEnableButtons() {
            return ResultController.GatherResults().Any();
        }
        
        // === Helpers
        bool IsDebugOrForRemoval(StoryGraph graph, string graphPath) {
            var graphTemplate = graph as ITemplate;
            bool isDebugPath = graphPath.Contains("Obsolete") || graphPath.Contains("Debug");
            bool isDebugTemplate = graphTemplate is { TemplateType: TemplateType.Debug or TemplateType.ForRemoval };
            return isDebugPath || isDebugTemplate;
        }

        bool IsBark(string graphPath) => graphPath.Contains("Bark");
        
        bool HasMissingEvent(SEditorText sText, out string audioEventPath) {
            var audioEventGuid = sText.AudioClipEventGUID;
            // EditorEventRef editorEventRef = EventManager.EventFromGUID(audioEventGuid);
            // audioEventPath = editorEventRef == null ? string.Empty : editorEventRef.Path;
            audioEventPath = ""; 
            return string.IsNullOrEmpty(audioEventPath);
        }
        
        bool HasMissingClip(SEditorText sText, out string audioClipName) {
            var audioFilePath = sText.GetAudioFilePath();
            var id = Path.GetFileNameWithoutExtension(audioFilePath).Replace(EditorAudioUtils.VoiceOverIdSeparator, '/');
            TableEntry tableEntry = LocalizationHelper.GetTableEntry(id, LocalizationSettings.ProjectLocale);
            var audioMeta = tableEntry.GetMetadata<AudioReplacementName>();
            audioClipName = audioMeta != null ? audioMeta.AudioReplacement : string.Empty;
            return string.IsNullOrEmpty(audioClipName);
        }
        
    }

    [Serializable]
    public class MissingVOEntry : DefaultResultEntry {
        [SerializeField, ReadOnly, UsedImplicitly]
        string actorName;

        [SerializeField, ReadOnly, UsedImplicitly]
        string dialogueLine;

        [SerializeField, ReadOnly, UsedImplicitly]
        string eventRef;

        [SerializeField, ReadOnly, UsedImplicitly]
        string audioClipName;
        
        public string ActorName => actorName;

        public MissingVOEntry(string actorName, string dialogueLine, string eventRef, string audioClipName, NodeGraph graph, StoryNode node) :
            base(graph, node) {
            this.actorName = actorName;
            this.dialogueLine = dialogueLine;
            this.eventRef = eventRef;
            this.audioClipName = audioClipName;
        }
    }

    [Flags]
    internal enum FilterMode : byte {
        All = GraphDuplicates | ActorDuplicates | Barks | Debug | MissingClips | MissingEvents,
        DefaultView = MissingEvents | MissingClips | GraphDuplicates | ActorDuplicates,
        None = 0,
        GraphDuplicates = 1 << 0, 
        ActorDuplicates = 1 << 1,
        Barks = 1 << 2,      
        Debug = 1 << 3,
        MissingClips = 1 << 4,   
        MissingEvents = 1 << 5,
    }

    internal enum SortMode : byte {
        Graph = 0,
        Actor = 1,
    }
}