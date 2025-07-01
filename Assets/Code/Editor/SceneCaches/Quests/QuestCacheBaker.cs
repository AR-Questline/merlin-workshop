using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.Quests {
    public class QuestCacheBaker : SceneBaker<QuestCache> {
        protected override QuestCache LoadCache => QuestCache.Get;

        public override void Bake(SceneReference sceneRef) {
            foreach (var (story, go) in CacheBakerUtils.ForEachStory(sceneRef)) {
                StoryGraph graph = null;
                try {
                    graph = story.EDITOR_Graph;
                } catch (Exception e) {
                    Debug.LogException(e);
                }

                var source = new QuestSource(go) {
                    storyGraphTemplate = graph != null ? new TemplateReference(graph) : null,
                };

                foreach (var step in StoryExplorerUtil.ExtractElements(story)) {
                    if (step is SEditorQuestAdd { questRef: { IsSet: true } } questAdd) {
                        if (questAdd.questRef.Get<QuestTemplate>() != null) {
                            source.data.Add(new QuestChangeData(questAdd.questRef, QuestChangeData.ChangeType.Start));
                        }
                    } else if (step is SEditorQuestComplete { questTemplate: { IsSet: true } } questComplete) {
                        if (questComplete.questTemplate.Get<QuestTemplate>() != null) {
                            source.data.Add(new QuestChangeData(questComplete.questTemplate, QuestChangeData.ChangeType.Complete));
                        }
                    } else if (step is SEditorQuestFail { questTemplate: { IsSet: true } } questFail) {
                        if (questFail.questTemplate.Get<QuestTemplate>() != null) {
                            source.data.Add(new QuestChangeData(questFail.questTemplate, QuestChangeData.ChangeType.Fail));
                        }
                    } else if (step is SEditorObjectiveChange { questRef: { IsSet: true } } objectiveChange) {
                        if (objectiveChange.questRef.Get<QuestTemplate>() != null) {
                            var objectiveStateToChangeType = ObjectiveStateToChangeType(objectiveChange.newState);
                            source.data.Add(new QuestChangeData(objectiveChange.questRef, objectiveChange.objectiveGuid, objectiveStateToChangeType));
                        }
                    }

                    foreach (var condition in step.ConditionNodes().SelectMany(n => n.elements)) {
                        if (condition is CEditorQuestState { questRef: { IsSet: true } } questState) {
                            if (questState.questRef.Get<QuestTemplate>() != null) {
                                source.data.Add(new QuestChangeData(questState.questRef, QuestChangeData.ChangeType.Condition));
                            }
                        } else if (condition is CEditorQuestObjective { questRef: { IsSet: true } } objectiveState) {
                            if (objectiveState.questRef.Get<QuestTemplate>() != null) {
                                source.data.Add(new QuestChangeData(objectiveState.questRef, objectiveState.objectiveGuid, QuestChangeData.ChangeType.Condition));
                            }
                        }
                    }
                }

                if (source.data.Any()) {
                    Cache.questSources.Add(source);
                }
            }
        }

        static QuestChangeData.ChangeType ObjectiveStateToChangeType(ObjectiveState state) {
            return state switch {
                ObjectiveState.Active => QuestChangeData.ChangeType.Start,
                ObjectiveState.Completed => QuestChangeData.ChangeType.Complete,
                ObjectiveState.Failed => QuestChangeData.ChangeType.Fail,
                _ => QuestChangeData.ChangeType.None
            };
        }

        public static void GenerateRegionFilters(OnDemandCache<string, ExportRegionFilter> cache) {
            OnDemandCache<string, HashSet<string>> scenesUsedByQuest = new(_ => new HashSet<string>(50));
            
            foreach (var source in QuestCache.Get.questSources) {
                foreach (var data in source.data) {
                    string guid = data.questTemplate?.GUID;
                    if (guid != null) {
                        if (scenesUsedByQuest[guid].Add(source.SceneName)) {
                            string region = source.OpenWorldRegion;
                            cache[guid] |= RegionFilterUtil.GetRegionFrom(region);
                        }
                    }
                }
            }
        }

        [MenuItem("TG/Design/Exp/Analyze HOS Quests")]
        static void AnalyzeHosQuests() {
            AnalyzeQuests("CampaignMap_HOS");
        }
        
        [MenuItem("TG/Design/Exp/Analyze Cuanacht Quests")]
        static void AnalyzeCuanachtQuests() {
            AnalyzeQuests("CampaignMap_Cuanacht");
        }
        
        [MenuItem("TG/Design/Exp/Analyze Forlorn Quests")]
        static void AnalyzeForlornQuests() {
            AnalyzeQuests("CampaignMap_Forlorn");
        }
        
        static void AnalyzeQuests(string region) {
            float exp = 0f;

            var cache = new Dictionary<string, float>();
            
            foreach (var source in QuestCache.Get.questSources.Where(s => s.OpenWorldRegion == region)) {
                foreach (var questData in source.data.Where(d => d.changeType == QuestChangeData.ChangeType.Complete)) {
                    QuestTemplate template = questData.QuestTemplate;
                    if (questData.HasObjective) {
                        var objective = template.GetComponentsInChildren<ObjectiveSpec>().FirstOrDefault(o => o.Guid == questData.objectiveGuid);
                        if (objective != null) {
                            string key = $"{template.name}_{questData.objectiveName}, {objective.TargetLevel}, {objective.ExperienceGainRange}";
                            if (!cache.ContainsKey(key)) {
                                var range = objective.CalculatedExpRange;
                                exp += range.min;
                                cache[key] = range.min;
                            }
                        }
                    } else {
                        string key = $"{template.name}, {template.targetLvl}, {template.xpGainRange}";
                        if (!cache.ContainsKey(key)) {
                            var range = template.CalculatedExpRange;
                            exp += range.min;
                            cache[key] = range.min;
                        }
                    }
                }
            }

            float totalExp = exp;
            Log.Important?.Error($"Total: {totalExp} exp");
            int lvl = 1;
            float expForNextLvl = HeroDevelopment.RequiredExpFor(lvl + 1);
            
            while (exp > expForNextLvl) {
                exp -= expForNextLvl;
                lvl++;
                expForNextLvl = HeroDevelopment.RequiredExpFor(lvl + 1);
            }
            
            Log.Important?.Error($"Total Lvl: {lvl}");
            
            var builder = new StringBuilder();
            builder.AppendLine($"Total Exp: {totalExp}");
            builder.AppendLine($"Total Lvl: {lvl}");
            builder.AppendLine();
            
            foreach (var kvp in cache.OrderByDescending(kvp => kvp.Value)) {
                builder.AppendLine($"{kvp.Key}, {kvp.Value}");
            }
            File.WriteAllText($"{Application.dataPath}/{region}_exp_report.txt", builder.ToString());
            Log.Important?.Error("Created report in Assets directory");
        }
    }
}