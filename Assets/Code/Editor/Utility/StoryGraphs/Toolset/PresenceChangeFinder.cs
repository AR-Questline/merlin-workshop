using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Editor.Utility.RichLabels;
using Awaken.TG.Editor.Utility.RichLabels.Configs;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all modifications of a presence")]
    public class PresenceChangeFinder : StoryGraphUtilityTool<SearchResult<PresenceChangeEntry>, PresenceChangeEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField, Required]
        RichLabelUsage label = new RichLabelUsage(RichLabelConfigType.Presence);

        RichLabelConfig richLabelConfig;
        
        protected override bool Validate() => label.RichLabelUsageEntries.Length > 0;

        protected override void ExecuteTool() {
            richLabelConfig = RichLabelEditorUtilities.GetOrCreateRichLabelConfig(label.Editor_ConfigType);
            foreach (var valueTuple in GatherAllFlagChange()) {
                ResultController.Feed(new PresenceChangeEntry(valueTuple.graph, valueTuple.node, valueTuple.mode, valueTuple.travel, valueTuple.richLabel));
            }
        }

        IEnumerable<(NodeGraph graph, StoryNode node, string mode, string travel, string richLabel)> GatherAllFlagChange() {
            return AllElementsWithInterface<StoryNode, SEditorActivateNpcPresenceViaRichLabels>()
                   .Where(trio => !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.FlagUsage)
                                  && ((SEditorActivateNpcPresenceViaRichLabels) trio.element).richLabelUsage.Matches(label))
                   .Select(trio => {
                       var step = (SEditorActivateNpcPresenceViaRichLabels) trio.element;
                       string richLabel = RichLabelEditorUtilities.RichLabelEntriesToString(richLabelConfig, step.richLabelUsage.RichLabelUsageEntries);
                       return (trio.graph,
                               trio.node,
                               step.mode switch {
                                   Mode.SetAvailable => "+ Available",
                                   Mode.SetUnavailable => "- Unavailable",
                                   _ => "Unknown"
                               },
                               step.travel.ToStringFast(),
                               richLabel);
                   }).OrderBy(data => data.richLabel, StringComparer.OrdinalIgnoreCase);
        }
    }
    
    [Serializable]
    public class PresenceChangeEntry : DefaultResultEntry {
        [SerializeField, DisplayAsString, TableColumnWidth(330)] string richLabel;
        [SerializeField, DisplayAsString, TableColumnWidth(80), GUIColor(nameof(ModeColor))] string mode;
        [SerializeField, DisplayAsString, TableColumnWidth(80)] string travel;
        
        public PresenceChangeEntry(NodeGraph graph, StoryNode node, string mode, string travel, string richLabel) : base(graph, node){
            this.mode = mode;
            this.travel = travel;
            this.richLabel = richLabel;
        }

        Color ModeColor => mode.StartsWith('+') ? GUIColors.Green : GUIColors.White;
    };
}
