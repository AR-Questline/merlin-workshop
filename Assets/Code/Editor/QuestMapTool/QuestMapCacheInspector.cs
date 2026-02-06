using Awaken.Utility.Debugging;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.QuestMapTool {
    /// <summary>
    /// Inspector for viewing Quest Map Tool Cache contents in detail.
    /// </summary>
    [CustomEditor(typeof(QuestMapCache))]
    public class QuestMapCacheInspector : UnityEditor.Editor {
        readonly StringBuilder _sharedStringBuilder = new();
        public override void OnInspectorGUI() {
            var cache = (QuestMapCache)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quest Map Tool Cache Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // === Statistics ===
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total NPCs: {cache.totalNpcs}");
            EditorGUILayout.LabelField($"Total Quests: {cache.totalQuests}");
            EditorGUILayout.LabelField($"Total Stories: {cache.totalStories}");
            EditorGUILayout.LabelField($"Total Scenes: {cache.totalScenes}");
            EditorGUILayout.LabelField($"Scanned Folder: {cache.scannedFolderPath}");
            EditorGUILayout.LabelField($"Last Built: {cache.lastBuiltTime}");
            EditorGUILayout.Space();

            // === Mappings ===
            EditorGUILayout.LabelField("Mappings", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"NPC → Scenes: {cache.npcToScenes.Count} entries");
            EditorGUILayout.LabelField($"NPC → Stories: {cache.npcToStories.Count} entries");
            EditorGUILayout.LabelField($"NPC → Quests: {cache.npcToQuests.Count} entries");
            EditorGUILayout.LabelField($"Quest → Scenes: {cache.questToScenes.Count} entries");
            EditorGUILayout.LabelField($"Quest Dependencies: {cache.questDependencies.Count} entries");
            EditorGUILayout.LabelField($"Scene → NPCs: {cache.sceneToNpcs.Count} entries");
            EditorGUILayout.Space();

            // === Debug Buttons ===
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Print All NPCs to Console")) {
                PrintAllNpcs(cache);
            }

            if (GUILayout.Button("Print All Quests to Console")) {
                PrintAllQuests(cache);
            }

            if (GUILayout.Button("Print All Stories to Console")) {
                PrintAllStories(cache);
            }

            if (GUILayout.Button("Print All Scenes to Console")) {
                PrintAllScenes(cache);
            }

            if (GUILayout.Button("Print NPC→Quest Mappings")) {
                PrintNpcQuestMappings(cache);
            }

            if (GUILayout.Button("Print Quest Dependencies")) {
                PrintQuestDependencies(cache);
            }

            EditorGUILayout.Space();

            // === Default Inspector ===
            EditorGUILayout.LabelField("Raw Data", EditorStyles.boldLabel);
            DrawDefaultInspector();
        }

        void PrintAllNpcs(QuestMapCache cache) {
            _sharedStringBuilder.Clear();
            _sharedStringBuilder.AppendLine("=== ALL NPCs IN CACHE ===");
            
            foreach (var npc in cache.allNpcs) {
                var sceneCount = cache.npcToScenes.ContainsKey(npc.guid) ? cache.npcToScenes[npc.guid].Count : 0;
                var storyCount = cache.npcToStories.ContainsKey(npc.guid) ? cache.npcToStories[npc.guid].Count : 0;
                var questCount = cache.npcToQuests.ContainsKey(npc.guid) ? cache.npcToQuests[npc.guid].Count : 0;
                
                _sharedStringBuilder.AppendLine($"  {npc.name} ({npc.guid})");
                _sharedStringBuilder.AppendLine($"    First Story: {npc.firstStoryName}");
                _sharedStringBuilder.AppendLine($"    Stories: {storyCount} | Quests: {questCount} | Scenes: {sceneCount}");
            }
            
            _sharedStringBuilder.AppendLine($"Total: {cache.allNpcs.Count} NPCs");
            Log.Important?.Info(_sharedStringBuilder.ToString());
        }

        void PrintAllQuests(QuestMapCache cache) {
            _sharedStringBuilder.Clear();
            _sharedStringBuilder.AppendLine("=== ALL QUESTS IN CACHE ===");
            
            foreach (var quest in cache.questCache.Values) {
                _sharedStringBuilder.AppendLine($"  [{quest.questType}] {quest.name}");
                _sharedStringBuilder.AppendLine($"    GUID: {quest.guid}");
                _sharedStringBuilder.AppendLine($"    NPCs: {quest.npcGuids.Count}");
                _sharedStringBuilder.AppendLine($"    Stories: {quest.storyGuids.Count}");
                _sharedStringBuilder.AppendLine($"    Objectives: {quest.objectives.Count}");
                _sharedStringBuilder.AppendLine($"    Scenes: {quest.sceneGuids.Count}");
                _sharedStringBuilder.AppendLine($"    Flags Used: {quest.flagsUsed.Count}");
                _sharedStringBuilder.AppendLine($"    Has Branches: {quest.hasMultipleBranches}");
            }
            
            _sharedStringBuilder.AppendLine($"Total: {cache.questCache.Count} Quests");
            Log.Important?.Info(_sharedStringBuilder.ToString());
        }

        void PrintAllStories(QuestMapCache cache) {
            _sharedStringBuilder.Clear();
            _sharedStringBuilder.AppendLine("=== ALL STORIES IN CACHE ===");
            
            foreach (var story in cache.storyGraphCache.Values) {
                _sharedStringBuilder.AppendLine($"  {story.name}");
                _sharedStringBuilder.AppendLine($"    GUID: {story.guid}");
                _sharedStringBuilder.AppendLine($"    Actors: {story.actorGuids.Count}");
                
                if (story.actorGuids.Count > 0) {
                    foreach (var actorGuid in story.actorGuids) {
                        var npc = cache.allNpcs.Find(n => n.guid == actorGuid);
                        _sharedStringBuilder.AppendLine($"      - {npc?.name ?? "Unknown"} ({actorGuid})");
                    }
                }
                
                _sharedStringBuilder.AppendLine($"    Quests: {story.questGuids.Count}");
                if (story.questGuids.Count > 0) {
                    foreach (var questGuid in story.questGuids) {
                        if (cache.questCache.TryGetValue(questGuid, out var quest)) {
                            _sharedStringBuilder.AppendLine($"      - {quest.name} ({questGuid})");
                        } else {
                            _sharedStringBuilder.AppendLine($"      - Unknown Quest ({questGuid})");
                        }
                    }
                }
            }
            
            _sharedStringBuilder.AppendLine($"Total: {cache.storyGraphCache.Count} Stories");
            Log.Important?.Info(_sharedStringBuilder.ToString());
        }

        void PrintAllScenes(QuestMapCache cache) {
            _sharedStringBuilder.Clear();
            _sharedStringBuilder.AppendLine("=== ALL SCENES IN CACHE ===");
            
            foreach (var kvp in cache.sceneToNpcs) {
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(kvp.Key);
                _sharedStringBuilder.AppendLine($"  {sceneName}");
                _sharedStringBuilder.AppendLine($"    Path: {kvp.Key}");
                _sharedStringBuilder.AppendLine($"    NPCs: {kvp.Value.Count}");
                
                foreach (var npc in kvp.Value) {
                    var presenceType = npc.isManual ? "[Manual]" :
                                     !string.IsNullOrEmpty(npc.flagCondition) ? $"[Flag: {npc.flagCondition}]" :
                                     "[Always]";
                    _sharedStringBuilder.AppendLine($"      - {npc.npcName} {presenceType}");
                    if (!string.IsNullOrEmpty(npc.firstStoryName)) {
                        _sharedStringBuilder.AppendLine($"        Story Start: {npc.firstStoryName}");
                    }
                }
            }
            
            _sharedStringBuilder.AppendLine($"Total: {cache.sceneToNpcs.Count} Scenes");
            Log.Important?.Info(_sharedStringBuilder.ToString());
        }

        void PrintNpcQuestMappings(QuestMapCache cache) {
            _sharedStringBuilder.Clear();
            _sharedStringBuilder.AppendLine("=== NPC → QUEST MAPPINGS ===");
            
            foreach (var kvp in cache.npcToQuests) {
                var npc = cache.allNpcs.Find(n => n.guid == kvp.Key);
                var npcName = npc?.name ?? "Unknown";

                _sharedStringBuilder.AppendLine($"  {npcName} ({kvp.Key}):");
                foreach (var quest in kvp.Value) {
                    _sharedStringBuilder.AppendLine($"    - [{quest.questType}] {quest.questName}");
                }
            }
            
            _sharedStringBuilder.AppendLine($"Total: {cache.npcToQuests.Count} NPCs with quests");
            Log.Important?.Info(_sharedStringBuilder.ToString());
        }

        void PrintQuestDependencies(QuestMapCache cache) {
            _sharedStringBuilder.Clear();
            _sharedStringBuilder.AppendLine("=== QUEST DEPENDENCIES (Shared Flags) ===");
            
            foreach (var kvp in cache.questDependencies) {
                if (kvp.Value.Count == 0) {
                    continue;
                }

                if (cache.questCache.TryGetValue(kvp.Key, out var quest)) {
                    _sharedStringBuilder.AppendLine($"  [{quest.questType}] {quest.name}");
                    _sharedStringBuilder.AppendLine($"    Flags Used: {string.Join(", ", quest.flagsUsed)}");
                    _sharedStringBuilder.AppendLine($"    Affects {kvp.Value.Count} other quest(s):");

                    foreach (var relatedGuid in kvp.Value) {
                        if (cache.questCache.TryGetValue(relatedGuid, out var relatedQuest)) {
                            var sharedFlags = quest.flagsUsed.Intersect(relatedQuest.flagsUsed).ToList();
                            _sharedStringBuilder.AppendLine($"      - [{relatedQuest.questType}] {relatedQuest.name}");
                            _sharedStringBuilder.AppendLine($"        Shared Flags: {string.Join(", ", sharedFlags)}");
                        }
                    }
                }
            }
            
            int questsWithDeps = cache.questDependencies.Count(kvp => kvp.Value.Count > 0);
            _sharedStringBuilder.AppendLine($"Total: {questsWithDeps} quests have dependencies");
            Log.Important?.Info(_sharedStringBuilder.ToString());
        }
    }
}
