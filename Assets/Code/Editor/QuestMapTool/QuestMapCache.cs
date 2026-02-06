using Awaken.TG.Main.General.Caches;
using Awaken.Utility.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.QuestMapTool {
    /// <summary>
    /// Persistent cache for Quest Map Tool that stores pre-computed relationships between
    /// Quests, NPCs, Stories, and Scenes with full dependency tracking.
    /// </summary>
    public class QuestMapCache : BaseCache {
        static QuestMapCache s_cache;
        public static QuestMapCache Get => s_cache ??= LoadFromAssets<QuestMapCache>("6378342b16ded4247becba78d39a199c");

        // === NPC Mappings ===

        /// <summary>NPC GUID → Scenes where this NPC has presence</summary>
        [SerializeField]
        public SerializedDictionary<string, List<ScenePresenceEntry>> npcToScenes = new();

        /// <summary>NPC GUID → Stories that use this NPC</summary>
        [SerializeField]
        public SerializedDictionary<string, List<StoryGraphEntry>> npcToStories = new();

        /// <summary>NPC GUID → Quests involving this NPC</summary>
        [SerializeField]
        public SerializedDictionary<string, List<QuestEntry>> npcToQuests = new();

        // === Quest Mappings ===

        /// <summary>Quest GUID → Full parsed quest data</summary>
        [SerializeField]
        public SerializedDictionary<string, QuestData> questCache = new();

        /// <summary>Quest GUID → Scenes where it can be started or progressed</summary>
        [SerializeField]
        public SerializedDictionary<string, List<SceneEntry>> questToScenes = new();

        /// <summary>Quest GUID → Other quests that affect it (via shared flags)</summary>
        [SerializeField]
        public SerializedDictionary<string, List<string>> questDependencies = new();

        // === Story Mappings ===

        /// <summary>Story GUID → Parsed story graph data</summary>
        [SerializeField]
        public SerializedDictionary<string, StoryGraphData> storyGraphCache = new();

        // === Scene Mappings ===

        /// <summary>Scene path → NPCs present in this scene</summary>
        [SerializeField]
        public SerializedDictionary<string, List<NpcPresenceEntry>> sceneToNpcs = new();

        // === Lookup Indices ===

        /// <summary>All NPCs for fast name search (sorted alphabetically)</summary>
        [SerializeField]
        public List<NpcSearchEntry> allNpcs = new();

        /// <summary>All Quests for search (sorted alphabetically)</summary>
        [SerializeField]
        public List<QuestSearchEntry> allQuests = new();

        /// <summary>All Scenes for search</summary>
        [SerializeField]
        public List<SceneSearchEntry> allScenes = new();

        /// <summary>Folder path that was last scanned</summary>
        [SerializeField]
        public string scannedFolderPath;

        /// <summary>Timestamp of when cache was last built</summary>
        [SerializeField]
        public string lastBuiltTime;

        /// <summary>Statistics for display</summary>
        [SerializeField]
        public int totalNpcs;
        [SerializeField]
        public int totalScenes;
        [SerializeField]
        public int totalQuests;
        [SerializeField]
        public int totalStories;
        
        public override void Clear() {
            npcToScenes.Clear();
            npcToStories.Clear();
            npcToQuests.Clear();
            questCache.Clear();
            questToScenes.Clear();
            questDependencies.Clear();
            storyGraphCache.Clear();
            sceneToNpcs.Clear();
            allNpcs.Clear();
            allQuests.Clear();
            allScenes.Clear();
            scannedFolderPath = "";
            totalNpcs = 0;
            totalScenes = 0;
            totalQuests = 0;
            totalStories = 0;
        }
    }

    // === Data Structures ===

    [Serializable]
    public class NpcSearchEntry {
        public string guid;
        public string name;
        public string nameLower;  // For case-insensitive search
        public string templatePath;
        public string firstStoryGuid;  // First story where NPC appears
        public string firstStoryName;
    }

    [Serializable]
    public class QuestSearchEntry {
        public string guid;
        public string name;
        public string nameLower;
        public string questType;  // Main, Side, Misc
        public string assetPath;
    }

    [Serializable]
    public class SceneSearchEntry {
        public string scenePath;
        public string sceneName;
        public int npcCount;
    }

    [Serializable]
    public class ScenePresenceEntry {
        public string scenePath;
        public string sceneName;
        public bool isManual;
        public string flagCondition;
    }

    [Serializable]
    public class NpcPresenceEntry {
        public string npcGuid;
        public string npcName;
        public bool isManual;
        public string flagCondition;
        public string firstStoryGuid;  // Where this NPC's story starts (for QA)
        public string firstStoryName;
    }

    [Serializable]
    public class QuestData {
        public string guid;
        public string name;
        public string questType;  // Main, Side, Misc
        public List<string> npcGuids = new();
        public List<ObjectiveData> objectives = new();
        public List<string> sceneGuids = new();
        public List<string> storyGuids = new();
        public List<string> flagsUsed = new();  // All flags used/set by this quest
        public List<string> flagsRequired = new();  // Flags required to start
        public string assetPath;
        public bool hasMultipleBranches;  // High-level: does quest have branching?
    }

    [Serializable]
    public class ObjectiveData {
        public string guid;
        public string name;
        public string description;
        public string sceneGuid;
        public string sceneName;
        public string locationReference;
        public bool hasMarker;
        public List<string> prerequisites = new();  // Flag names
    }

    [Serializable]
    public class StoryGraphData {
        public string guid;
        public string name;
        public List<string> actorGuids = new();
        public List<string> questGuids = new();  // Quests started by this story
        public string assetPath;
    }

    [Serializable]
    public class StoryGraphEntry {
        public string storyGuid;
        public string storyName;
        public string storyPath;
    }

    [Serializable]
    public class QuestEntry {
        public string questGuid;
        public string questName;
        public string questType;
    }

    [Serializable]
    public class SceneEntry {
        public string sceneGuid;
        public string sceneName;
        public string scenePath;
        public string storyGuid;
        public string storyName;
    }
}
