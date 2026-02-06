using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.QuestMapTool {
    /// <summary>
    /// Builds comprehensive cache by scanning NPCs, Quests, Stories, and Scenes.
    /// </summary>
    public class QuestMapCacheBaker : SceneBaker<QuestMapCache> {
        protected override QuestMapCache LoadCache => QuestMapCache.Get;

        public override void StartBaking() {
            base.StartBaking();
            Log.Important?.Info("Quest Map Tool: Starting cache baking...");

            // Phase 1: Scan all NPCs from ActorsRegister
            Log.Important?.Info("  [1/4] Scanning NPCs...");
            ScanActors();

            // Phase 2: Scan all StoryGraphs
            Log.Important?.Info("  [2/4] Scanning StoryGraphs...");
            ScanStoryGraphs();

            // Phase 3: Scan all QuestTemplates
            Log.Important?.Info("  [3/4] Scanning QuestTemplates...");
            ScanQuestTemplates();

            // Phase 4: Integrate with existing QuestCache
            Log.Important?.Info("  [4/4] Integrating with existing QuestCache...");
            IntegrateWithQuestCache();
        }

        public override void Bake(SceneReference sceneRef) {
            var scenePath = sceneRef.LoadedScene.path;
            var sceneName = sceneRef.LoadedScene.name;
            Log.Debug?.Info($"      Scanning scene: {sceneName}");

            Cache.totalScenes++;

            if (!Cache.sceneToNpcs.ContainsKey(scenePath)) {
                Cache.sceneToNpcs[scenePath] = new List<NpcPresenceEntry>();
            }

            foreach (var rootObj in CacheBakerUtils.ForEachSceneGO()) {
                var npcPresenceAttachments = rootObj.GetComponentsInChildren<NpcPresenceAttachment>(true);

                foreach (var presenceAttachment in npcPresenceAttachments) {
                    var locationSpec = presenceAttachment.GetComponent<LocationSpec>();
                    if (locationSpec == null) {
                        continue;
                    }

                    var template = presenceAttachment.Template;
                    if (template == null) {
                        continue;
                    }

                    var uniqueNpcAttachment = template.GetComponent<UniqueNpcAttachment>();
                    if (uniqueNpcAttachment == null) {
                        continue;
                    }

                    var actorRef = uniqueNpcAttachment.GetActor();
                    if (actorRef.IsEmpty) {
                        continue;
                    }

                    var npcGuid = actorRef.guid;
                    var npcName = GetActorName(npcGuid);

                    if (string.IsNullOrEmpty(npcGuid)) {
                        continue;
                    }

                    // Get first story for this NPC
                    string firstStoryGuid = "";
                    string firstStoryName = "";
                    if (Cache.npcToStories.TryGetValue(npcGuid, out var stories) && stories.Count > 0) {
                        firstStoryGuid = stories[0].storyGuid;
                        firstStoryName = stories[0].storyName;
                    }

                    string flagCondition = "";
                    if (!presenceAttachment.Manual && presenceAttachment.FlagAvailability.HasFlag) {
                        flagCondition = presenceAttachment.FlagAvailability.Flag;
                    }

                    Cache.sceneToNpcs[scenePath].Add(new NpcPresenceEntry {
                        npcGuid = npcGuid,
                        npcName = npcName,
                        isManual = presenceAttachment.Manual,
                        flagCondition = flagCondition,
                        firstStoryGuid = firstStoryGuid,
                        firstStoryName = firstStoryName
                    });

                    if (!Cache.npcToScenes.ContainsKey(npcGuid)) {
                        Cache.npcToScenes[npcGuid] = new List<ScenePresenceEntry>();
                    }

                    Cache.npcToScenes[npcGuid].Add(new ScenePresenceEntry {
                        scenePath = scenePath,
                        sceneName = sceneName,
                        isManual = presenceAttachment.Manual,
                        flagCondition = flagCondition
                    });
                }
            }

            // Add to scene search index
            Cache.allScenes.Add(new SceneSearchEntry {
                scenePath = scenePath,
                sceneName = sceneName,
                npcCount = Cache.sceneToNpcs[scenePath].Count
            });
        }

        public override void FinishBaking() {
            Log.Important?.Info("Quest Map Tool: Building cross-references...");
            BuildCrossReferences();

            Cache.lastBuiltTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Log.Important?.Info("Quest Map Cache baked successfully! " +
                                $"NPCs: {Cache.totalNpcs}, " +
                                $"Quests: {Cache.totalQuests}, " +
                                $"Stories: {Cache.totalStories}, " +
                                $"Scenes: {Cache.totalScenes}");

            base.FinishBaking();
        }

        static string GetActorName(string actorGuid) {
            var actorName = ActorsRegister.Get.Editor_GetActorName(actorGuid);
            return !string.IsNullOrEmpty(actorName) ? actorName : "Unknown NPC";
        }

        // === Phase 1: Scan Actors ===

        void ScanActors() {
            foreach (var actorSpec in ActorsRegister.Get.AllActors) {
                if (actorSpec == null) continue;

                var displayName = actorSpec.displayName?.ToString() ?? actorSpec.name;
                if (string.IsNullOrWhiteSpace(displayName)) {
                    displayName = actorSpec.name;
                }

                if (string.IsNullOrWhiteSpace(displayName)) continue;

                var entry = new NpcSearchEntry {
                    guid = actorSpec.Guid,
                    name = displayName,
                    nameLower = displayName.ToLowerInvariant(),
                    templatePath = ActorsRegister.Path
                };

                Cache.allNpcs.Add(entry);
            }

            Cache.allNpcs.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            Cache.totalNpcs = Cache.allNpcs.Count;

            Log.Debug?.Info($"    Scanned {Cache.totalNpcs} actors");
        }

        // === Phase 2: Scan StoryGraphs ===

        void ScanStoryGraphs() {
            var guids = AssetDatabase.FindAssets("t:StoryGraph");
            Cache.totalStories = guids.Length;

            foreach (var guid in guids) {
                if (string.IsNullOrEmpty(guid)) continue;

                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var story = AssetDatabase.LoadAssetAtPath<StoryGraph>(path);
                if (story == null) continue;

                var storyGuid = story.GUID;
                if (string.IsNullOrEmpty(storyGuid)) {
                    storyGuid = guid;
                }

                var data = new StoryGraphData {
                    guid = storyGuid,
                    name = story.name,
                    actorGuids = story.allowedActors?.Where(a => !string.IsNullOrEmpty(a.guid))
                        .Select(a => a.guid)
                        .ToList() ?? new List<string>(),
                    assetPath = path
                };

                // Find all quest add nodes
                if (story.nodes != null) {
                    var questNodes = story.nodes.OfType<SEditorQuestAdd>().ToList();
                    foreach (var node in questNodes) {
                        if (node.questRef != null && !string.IsNullOrEmpty(node.questRef.GUID)) {
                            data.questGuids.Add(node.questRef.GUID);
                        }
                    }

                    // Follow graph jumps
                    var jumpNodes = story.nodes.OfType<SEditorGraphJump>().ToList();
                    var visitedStories = new HashSet<string> { storyGuid };
                    CollectQuestsFromChildStories(jumpNodes, data.questGuids, visitedStories);
                }

                Cache.storyGraphCache[storyGuid] = data;

                // Map NPCs → Stories
                foreach (var actorGuid in data.actorGuids) {
                    if (!Cache.npcToStories.ContainsKey(actorGuid)) {
                        Cache.npcToStories[actorGuid] = new List<StoryGraphEntry>();
                    }

                    Cache.npcToStories[actorGuid].Add(new StoryGraphEntry {
                        storyGuid = storyGuid,
                        storyName = story.name,
                        storyPath = path
                    });
                }
            }

            Log.Debug?.Info($"    Scanned {Cache.storyGraphCache.Count} stories");
        }

        static void CollectQuestsFromChildStories(List<SEditorGraphJump> jumpNodes, List<string> questGuids, HashSet<string> visitedStories) {
            foreach (var jumpNode in jumpNodes) {
                if (jumpNode.bookmark == null || jumpNode.bookmark.story == null || !jumpNode.bookmark.story.IsSet) {
                    continue;
                }

                var childStoryGuid = jumpNode.bookmark.story.GUID;

                if (!visitedStories.Add(childStoryGuid)) {
                    continue;
                }

                var childStoryPath = AssetDatabase.GUIDToAssetPath(childStoryGuid);
                var childStory = AssetDatabase.LoadAssetAtPath<StoryGraph>(childStoryPath);

                if (childStory == null || childStory.nodes == null) {
                    continue;
                }

                var childQuestNodes = childStory.nodes.OfType<SEditorQuestAdd>().ToList();
                foreach (var questNode in childQuestNodes) {
                    if (questNode.questRef != null && !string.IsNullOrEmpty(questNode.questRef.GUID)) {
                        if (!questGuids.Contains(questNode.questRef.GUID)) {
                            questGuids.Add(questNode.questRef.GUID);
                        }
                    }
                }

                var childJumpNodes = childStory.nodes.OfType<SEditorGraphJump>().ToList();
                if (childJumpNodes.Count > 0) {
                    CollectQuestsFromChildStories(childJumpNodes, questGuids, visitedStories);
                }
            }
        }

        // === Phase 3: Scan QuestTemplates ===

        void ScanQuestTemplates() {
            var allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Data/Templates/Quests" });
            Log.Debug?.Info($"    Found {allPrefabGuids.Length} prefabs in Quests folder");

            var questPrefabs = new List<GameObject>();
            foreach (var guid in allPrefabGuids) {
                if (string.IsNullOrEmpty(guid)) continue;

                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<QuestTemplate>() != null) {
                    questPrefabs.Add(prefab);
                }
            }

            Cache.totalQuests = questPrefabs.Count;
            Log.Debug?.Info($"    Found {questPrefabs.Count} quest prefabs");

            foreach (var questPrefab in questPrefabs) {
                if (questPrefab == null) continue;

                var quest = questPrefab.GetComponent<QuestTemplate>();
                if (quest == null) continue;

                var path = AssetDatabase.GetAssetPath(questPrefab);
                var questGuid = quest.GUID;
                if (string.IsNullOrEmpty(questGuid)) {
                    questGuid = AssetDatabase.AssetPathToGUID(path);
                }

                var data = new QuestData {
                    guid = questGuid,
                    name = quest.displayName?.ToString() ?? quest.name,
                    questType = quest.TypeOfQuest.ToString(),
                    assetPath = path
                };

                // Parse objectives
                var objectiveSpecs = quest.ObjectiveSpecs.value;
                if (objectiveSpecs != null && objectiveSpecs.Count > 0) {
                    data.hasMultipleBranches = objectiveSpecs.Count > 1;

                    foreach (ObjectiveSpecBase objSpecBase in objectiveSpecs) {
                        if (objSpecBase is ObjectiveSpec objSpec) {
                            string sceneGuid = "";
                            string sceneName = "";

                            if (objSpec.TargetScene != null && objSpec.TargetScene.TryGetSceneAssetGUID(out var sceneAssetGuid)) {
                                sceneGuid = sceneAssetGuid;
                                sceneName = objSpec.TargetScene.Name ?? "";
                            }

                            var objData = new ObjectiveData {
                                guid = objSpec.Guid,
                                name = objSpec.GetName(),
                                description = objSpec.Description?.ToString() ?? "",
                                sceneGuid = sceneGuid,
                                sceneName = sceneName,
                                locationReference = objSpec.TargetLocationReference?.ToString() ?? "",
                                hasMarker = objSpec.TargetLocationReference != null
                            };

                            // Extract flags
                            if (objSpec.IsMarkerRelatedToStory && objSpec.RelatedStoryFlag.HasFlag) {
                                var flag = objSpec.RelatedStoryFlag.Flag;
                                objData.prerequisites.Add(flag);
                                if (!data.flagsRequired.Contains(flag)) {
                                    data.flagsRequired.Add(flag);
                                }

                                if (!data.flagsUsed.Contains(flag)) {
                                    data.flagsUsed.Add(flag);
                                }
                            }

                            data.objectives.Add(objData);

                            if (!string.IsNullOrEmpty(objData.sceneGuid)) {
                                if (!data.sceneGuids.Contains(objData.sceneGuid)) {
                                    data.sceneGuids.Add(objData.sceneGuid);
                                }
                            }
                        }
                    }
                }

                Cache.questCache[questGuid] = data;

                // Add to search index
                Cache.allQuests.Add(new QuestSearchEntry {
                    guid = questGuid,
                    name = data.name,
                    nameLower = data.name.ToLowerInvariant(),
                    questType = data.questType,
                    assetPath = path
                });
            }

            Cache.allQuests.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            Log.Debug?.Info($"    Scanned {Cache.questCache.Count} quests");
        }

        // === Phase 4: Integrate with Existing QuestCache ===

        void IntegrateWithQuestCache() {
            var existingQuestCache = QuestCache.Get;
            if (existingQuestCache?.questSources == null || existingQuestCache.questSources.Count == 0) {
                Log.Important?.Warning("QuestCache not available or empty");
                return;
            }

            Log.Important?.Info($"    Found {existingQuestCache.questSources.Count} quest sources in existing cache");

            int integratedQuests = 0;

            foreach (var questSource in existingQuestCache.questSources) {
                var storyGuid = questSource.storyGraphTemplate?.GUID;
                if (string.IsNullOrEmpty(storyGuid)) {
                    continue;
                }

                if (!Cache.storyGraphCache.TryGetValue(storyGuid, out var storyData)) {
                    continue;
                }

                foreach (var questChange in questSource.data) {
                    var questGuid = questChange.questTemplate?.GUID;
                    if (string.IsNullOrEmpty(questGuid)) {
                        continue;
                    }

                    if (!storyData.questGuids.Contains(questGuid)) {
                        storyData.questGuids.Add(questGuid);
                        integratedQuests++;
                    }
                }
            }

            Log.Important?.Info($"    Integrated {integratedQuests} quests from existing QuestCache");
        }

        // === Build Cross-References ===

        void BuildCrossReferences() {
            // Build NPC → Quests via Stories
            int npcQuestMappings = 0;
            foreach (var storyData in Cache.storyGraphCache.Values) {
                foreach (var actorGuid in storyData.actorGuids) {
                    if (!Cache.npcToQuests.ContainsKey(actorGuid)) {
                        Cache.npcToQuests[actorGuid] = new List<QuestEntry>();
                    }

                    foreach (var questGuid in storyData.questGuids) {
                        if (Cache.questCache.TryGetValue(questGuid, out var questData)) {
                            if (!questData.npcGuids.Contains(actorGuid)) {
                                questData.npcGuids.Add(actorGuid);
                            }

                            var existingEntry = Cache.npcToQuests[actorGuid].FirstOrDefault(q => q.questGuid == questGuid);
                            if (existingEntry == null) {
                                Cache.npcToQuests[actorGuid].Add(new QuestEntry {
                                    questGuid = questGuid,
                                    questName = questData.name,
                                    questType = questData.questType
                                });
                                npcQuestMappings++;
                            }

                            if (!questData.storyGuids.Contains(storyData.guid)) {
                                questData.storyGuids.Add(storyData.guid);
                            }
                        }
                    }
                }
            }

            // Set first story for each NPC
            foreach (var npc in Cache.allNpcs) {
                if (Cache.npcToStories.TryGetValue(npc.guid, out var stories) && stories.Count > 0) {
                    npc.firstStoryGuid = stories[0].storyGuid;
                    npc.firstStoryName = stories[0].storyName;
                }
            }

            // Build quest → scene mappings
            foreach (var questData in Cache.questCache.Values) {
                if (!Cache.questToScenes.ContainsKey(questData.guid)) {
                    Cache.questToScenes[questData.guid] = new List<SceneEntry>();
                }

                foreach (var sceneGuid in questData.sceneGuids.Distinct()) {
                    if (string.IsNullOrEmpty(sceneGuid)) {
                        continue;
                    }

                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                    if (string.IsNullOrEmpty(scenePath)) {
                        continue;
                    }

                    var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    var relatedStory = questData.storyGuids.FirstOrDefault();
                    var storyName = "";

                    if (!string.IsNullOrEmpty(relatedStory) &&
                        Cache.storyGraphCache.TryGetValue(relatedStory, out var storyData)) {
                        storyName = storyData.name;
                    }

                    Cache.questToScenes[questData.guid].Add(new SceneEntry {
                        sceneGuid = sceneGuid,
                        sceneName = sceneName,
                        scenePath = scenePath,
                        storyGuid = relatedStory ?? "",
                        storyName = storyName
                    });
                }
            }

            // Build quest dependencies via shared flags
            BuildQuestDependencies();

            Log.Important?.Info($"    Built {npcQuestMappings} NPC→Quest mappings");
        }

        void BuildQuestDependencies() {
            // For each quest, find other quests that use the same flags
            foreach (var quest1 in Cache.questCache.Values) {
                if (quest1.flagsUsed.Count == 0) {
                    continue;
                }

                Cache.questDependencies[quest1.guid] = new List<string>();

                foreach (var quest2 in Cache.questCache.Values) {
                    if (quest1.guid == quest2.guid) {
                        continue;
                    }

                    // Check if quest2 uses any of quest1's flags
                    var sharedFlags = quest1.flagsUsed.Intersect(quest2.flagsUsed).ToList();
                    if (sharedFlags.Count > 0) {
                        Cache.questDependencies[quest1.guid].Add(quest2.guid);
                    }
                }
            }

            int questsWithDeps = Cache.questDependencies.Count(kvp => kvp.Value.Count > 0);
            Log.Important?.Info($"    Built quest dependencies: {questsWithDeps} quests have shared flags");
        }
    }
}