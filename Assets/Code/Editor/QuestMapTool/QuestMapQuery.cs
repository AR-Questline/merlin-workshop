using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Awaken.TG.Editor.QuestMapTool {
    /// <summary>
    /// Helper utilities for querying the Quest Map cache with 3 search modes.
    /// </summary>
    public static class QuestMapQuery {
        static QuestMapCache _cache;

        /// <summary>
        /// Gets the current cache instance, loading it if necessary.
        /// </summary>
        public static QuestMapCache GetCache() {
            if (_cache == null) {
                _cache = QuestMapCache.Get;
            }

            return _cache;
        }

        /// <summary>
        /// Clears the cached reference, forcing reload on next access.
        /// </summary>
        public static void ClearCachedReference() {
            _cache = null;
        }

        /// <summary>
        /// Checks if cache exists and is not empty.
        /// </summary>
        public static bool IsCacheValid() {
            var cache = GetCache();
            return cache != null && cache.totalNpcs > 0;
        }

        // === NPC Queries ===

        /// <summary>
        /// Searches for NPCs by name (case-insensitive, partial match).
        /// </summary>
        public static List<NpcSearchEntry> SearchNpcsByName(string searchTerm) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(searchTerm)) {
                return new List<NpcSearchEntry>();
            }

            var lowerSearch = searchTerm.ToLowerInvariant();
            return cache.allNpcs.Where(npc => npc.nameLower.Contains(lowerSearch)).ToList();
        }

        /// <summary>
        /// Gets an NPC by its GUID.
        /// </summary>
        public static NpcSearchEntry GetNpcByGuid(string guid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(guid)) {
                return null;
            }

            return cache.allNpcs.FirstOrDefault(npc => npc.guid == guid);
        }

        /// <summary>
        /// Gets all NPCs sorted alphabetically.
        /// </summary>
        public static List<NpcSearchEntry> GetAllNpcs() {
            var cache = GetCache();
            return cache?.allNpcs ?? new List<NpcSearchEntry>();
        }

        /// <summary>
        /// Gets all stories that involve a specific NPC.
        /// </summary>
        public static List<StoryGraphEntry> GetStoriesForNpc(string npcGuid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(npcGuid)) {
                return new List<StoryGraphEntry>();
            }

            if (cache.npcToStories.TryGetValue(npcGuid, out var stories)) {
                return stories;
            }

            return new List<StoryGraphEntry>();
        }

        /// <summary>
        /// Gets all quests that involve a specific NPC.
        /// </summary>
        public static List<QuestEntry> GetQuestsForNpc(string npcGuid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(npcGuid)) {
                return new List<QuestEntry>();
            }

            if (cache.npcToQuests.TryGetValue(npcGuid, out var quests)) {
                return quests;
            }

            return new List<QuestEntry>();
        }

        /// <summary>
        /// Gets all scenes where a specific NPC has presence.
        /// </summary>
        public static List<ScenePresenceEntry> GetScenesForNpc(string npcGuid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(npcGuid)) {
                return new List<ScenePresenceEntry>();
            }

            if (cache.npcToScenes.TryGetValue(npcGuid, out var scenes)) {
                return scenes;
            }

            return new List<ScenePresenceEntry>();
        }

        // === Quest Queries ===

        /// <summary>
        /// Searches for quests by name (case-insensitive, partial match).
        /// </summary>
        public static List<QuestSearchEntry> SearchQuestsByName(string searchTerm) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(searchTerm)) {
                return new List<QuestSearchEntry>();
            }

            var lowerSearch = searchTerm.ToLowerInvariant();
            return cache.allQuests.Where(quest => quest.nameLower.Contains(lowerSearch)).ToList();
        }

        /// <summary>
        /// Gets quest data by GUID.
        /// </summary>
        public static QuestData GetQuestData(string questGuid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(questGuid)) {
                return null;
            }

            cache.questCache.TryGetValue(questGuid, out var data);
            return data;
        }

        /// <summary>
        /// Gets all quests sorted alphabetically.
        /// </summary>
        public static List<QuestSearchEntry> GetAllQuests() {
            var cache = GetCache();
            return cache?.allQuests ?? new List<QuestSearchEntry>();
        }

        /// <summary>
        /// Gets all scenes where a quest can be started or progressed.
        /// </summary>
        public static List<SceneEntry> GetScenesForQuest(string questGuid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(questGuid)) {
                return new List<SceneEntry>();
            }

            if (cache.questToScenes.TryGetValue(questGuid, out var scenes)) {
                return scenes;
            }

            return new List<SceneEntry>();
        }

        /// <summary>
        /// Gets other quests that affect this quest (via shared flags).
        /// </summary>
        public static List<string> GetRelatedQuests(string questGuid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(questGuid)) {
                return new List<string>();
            }

            if (cache.questDependencies.TryGetValue(questGuid, out var relatedQuests)) {
                return relatedQuests;
            }

            return new List<string>();
        }

        /// <summary>
        /// Gets NPCs involved in a specific quest.
        /// </summary>
        public static List<NpcSearchEntry> GetNpcsForQuest(string questGuid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(questGuid)) {
                return new List<NpcSearchEntry>();
            }

            if (!cache.questCache.TryGetValue(questGuid, out var questData)) {
                return new List<NpcSearchEntry>();
            }

            var npcs = new List<NpcSearchEntry>();
            foreach (var npcGuid in questData.npcGuids) {
                var npc = cache.allNpcs.FirstOrDefault(n => n.guid == npcGuid);
                if (npc != null) {
                    npcs.Add(npc);
                }
            }

            return npcs;
        }

        // === Story Queries ===

        /// <summary>
        /// Gets story data by GUID.
        /// </summary>
        public static StoryGraphData GetStoryData(string storyGuid) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(storyGuid)) {
                return null;
            }

            cache.storyGraphCache.TryGetValue(storyGuid, out var data);
            return data;
        }

        // === Scene Queries ===

        /// <summary>
        /// Gets all NPCs present in a specific scene.
        /// </summary>
        public static List<NpcPresenceEntry> GetNpcsForScene(string scenePath) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(scenePath)) {
                return new List<NpcPresenceEntry>();
            }

            if (cache.sceneToNpcs.TryGetValue(scenePath, out var npcs)) {
                return npcs;
            }

            return new List<NpcPresenceEntry>();
        }

        /// <summary>
        /// Searches for scenes by name (case-insensitive, partial match).
        /// </summary>
        public static List<SceneSearchEntry> SearchScenesByName(string searchTerm) {
            var cache = GetCache();
            if (cache == null || string.IsNullOrEmpty(searchTerm)) {
                return new List<SceneSearchEntry>();
            }

            var lowerSearch = searchTerm.ToLowerInvariant();
            return cache.allScenes.Where(scene => scene.sceneName.ToLowerInvariant().Contains(lowerSearch)).ToList();
        }

        /// <summary>
        /// Gets all scenes sorted alphabetically.
        /// </summary>
        public static List<SceneSearchEntry> GetAllScenes() {
            var cache = GetCache();
            return cache?.allScenes ?? new List<SceneSearchEntry>();
        }

        /// <summary>
        /// Gets the folder path that was last scanned.
        /// </summary>
        public static string GetScannedFolderPath() {
            var cache = GetCache();
            return cache?.scannedFolderPath ?? "";
        }

        // === Statistics ===

        /// <summary>
        /// Gets cache statistics for display.
        /// </summary>
        public static (int npcs, int quests, int stories, int scenes, string lastBuilt, string folder) GetCacheStats() {
            var cache = GetCache();
            if (cache == null) {
                return (0, 0, 0, 0, "Never", "");
            }

            return (cache.totalNpcs, cache.totalQuests, cache.totalStories, cache.totalScenes,
                    cache.lastBuiltTime ?? "Never", cache.scannedFolderPath ?? "");
        }
    }
}
