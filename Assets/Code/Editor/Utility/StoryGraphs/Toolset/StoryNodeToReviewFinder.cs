using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility.RichLabels;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all to review nodes in all story graphs")]
    public class StoryNodeTasksFinder : StoryGraphUtilityTool<DefaultResult<DefaultResultEntry>, DefaultResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder), LabelText("Show 'to review'")]
        [SerializeField] 
        bool showToReview;
        
        [BoxGroup(InputSectionName), PropertyOrder(InputSectionOrder), DisableIf(nameof(showToReview))]
        [SerializeField] 
        RichLabelUsage noteType = new(RichLabelConfigType.StoryTask);
        
        [BoxGroup(InputSectionName), PropertyOrder(InputSectionOrder), DisableIf(nameof(showToReview)), DisableIf(nameof(unrelatedToQuests))]
        [HorizontalGroup(InputSectionName + "/Filtering")]
        [SerializeField]
        QuestTemplate relatedToQuest;
        
        [BoxGroup(InputSectionName), PropertyOrder(InputSectionOrder), DisableIf(nameof(showToReview))]
        [HorizontalGroup(InputSectionName + "/Filtering")]
        [SerializeField]
        bool unrelatedToQuests;
        
        protected override bool Validate() {
            noteType ??= new RichLabelUsage(RichLabelConfigType.StoryTask);
            return true;
        }
        
        protected override void ExecuteTool() {
            if (showToReview) {
                AllNodes<StoryNode>()
                    .Where(nodePair => !nodePair.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.StoryNodeTasks) 
                                       && nodePair.node.toReview)
                    .OrderBy(nodePair => nodePair.graph.name)
                    .ForEach(nodePair => ResultController.Feed(new DefaultResultEntry(nodePair.graph, nodePair.node)));
            } else {
                var tasks = GatherTasks();
                if (unrelatedToQuests) {
                    var graphsWithQuest = AllElementsWithInterface<StoryNode, IStoryQuestRef>()
                                          .Select(trio => trio.graph).ToHashSet();
                    tasks = tasks.Where(task => !graphsWithQuest.Contains(task.graph));
                } else if (relatedToQuest != null) {
                    TemplateReference templateReference = new(relatedToQuest);
                    var graphsWithQuest = AllElementsWithInterface<StoryNode, IStoryQuestRef>()
                                          .Where(trio => ((IStoryQuestRef) trio.element).QuestRef.Equals(templateReference))
                                          .Select(trio => trio.graph).ToHashSet();

                    tasks = tasks.Where(task => graphsWithQuest.Contains(task.graph));
                }

                tasks.ForEach(nodePair => ResultController.Feed(new DefaultResultEntry(nodePair.graph, nodePair.node, nodePair.node.comment)));
            }
        }

        IEnumerable<GraphNodePair<TaskNode>> GatherTasks() {
            var config = RichLabelEditorUtilities.GetOrCreateRichLabelConfig(RichLabelConfigType.StoryTask);
            return AllNodes<TaskNode>()
                .Where(nodePair => {
                    RichLabelSet nodeTaskLabels = nodePair.node.taskLabels;
                    if (nodeTaskLabels?.richLabelGuids == null || nodeTaskLabels.richLabelGuids.Count == 0 || nodeTaskLabels.configType != RichLabelConfigType.StoryTask) {
                        return false;
                    }
                    var labels = config.TryGetSavedLabels(nodeTaskLabels);
                    if (labels == null || labels.Length == 0) {
                        return false;
                    }

                    return noteType.RichLabelUsageEntries.All(e => {
                        if (e.Include) {
                            return labels.Any(l => l?.Guid == e.RichLabelGuid);
                        }
                        return labels.All(l => l?.Guid != e.RichLabelGuid);
                    });
                })
                .OrderBy(nodePair => nodePair.graph.name);
        }
    }
}
