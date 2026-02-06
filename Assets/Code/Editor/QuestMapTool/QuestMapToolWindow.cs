using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.QuestMapTool {
    /// <summary>
    /// Main editor window for Quest Map Tool with 3 search modes: Quest, NPC, Scene.
    /// </summary>
    public class QuestMapToolWindow : EditorWindow {
        // === Search Modes ===
        enum SearchMode {
            Quest,
            NPC,
            Scene
        }

        // === UI Elements ===
        TextField _folderPathField;
        Button _browseFolderButton;
        Button _buildCacheButton;
        Label _cacheInfoLabel;

        Button _questModeButton;
        Button _npcModeButton;
        Button _sceneModeButton;

        TextField _searchField;
        Button _searchButton;
        Button _listAllButton;

        ScrollView _resultsContainer;
        Label _statusLabel;

        // === State ===
        SearchMode _currentMode = SearchMode.NPC;
        string _currentSearchTerm = "";

        // === Colors ===
        static readonly Color ActiveModeColor = new Color(0.3f, 0.6f, 1f);
        static readonly Color InactiveModeColor = new Color(0.4f, 0.4f, 0.4f);

        [MenuItem("TG/Design/Quest Map Tool")]
        public static void ShowWindow() {
            var window = GetWindow<QuestMapToolWindow>();
            window.titleContent = new GUIContent("Quest Map Tool");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        void CreateGUI() {
            var root = rootVisualElement;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.flexGrow = 1;

            // === Header: Folder Selection ===
            CreateFolderSection(root);

            // === Cache Info Bar ===
            _cacheInfoLabel = new Label();
            _cacheInfoLabel.style.marginBottom = 8;
            _cacheInfoLabel.style.marginTop = 4;
            _cacheInfoLabel.style.fontSize = 10;
            _cacheInfoLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            root.Add(_cacheInfoLabel);
            UpdateCacheInfo();

            // === Search Mode Selector ===
            CreateModeSelector(root);

            // === Search Bar ===
            CreateSearchBar(root);

            // === Status Label ===
            _statusLabel = new Label("Select a folder and click 'Scan Scenes' to begin");
            _statusLabel.style.marginBottom = 4;
            _statusLabel.style.marginTop = 4;
            _statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _statusLabel.style.fontSize = 11;
            root.Add(_statusLabel);

            // === Results Container ===
            _resultsContainer = new ScrollView();
            _resultsContainer.style.flexGrow = 1;
            root.Add(_resultsContainer);

            // Initial check
            if (!QuestMapQuery.IsCacheValid()) {
                ShowNoCacheWarning();
            }
        }

        // === UI Creation Methods ===

        void CreateFolderSection(VisualElement root) {
            var folderSection = new VisualElement();
            folderSection.style.marginBottom = 8;
            folderSection.style.paddingTop = 8;
            folderSection.style.paddingBottom = 8;
            folderSection.style.paddingLeft = 8;
            folderSection.style.paddingRight = 8;
            folderSection.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
            folderSection.style.borderBottomLeftRadius = 4;
            folderSection.style.borderBottomRightRadius = 4;
            folderSection.style.borderTopLeftRadius = 4;
            folderSection.style.borderTopRightRadius = 4;

            var folderLabel = new Label("Scan Folder");
            folderLabel.style.fontSize = 11;
            folderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            folderLabel.style.marginBottom = 4;
            folderSection.Add(folderLabel);

            var folderRow = new VisualElement();
            folderRow.style.flexDirection = FlexDirection.Row;

            _folderPathField = new TextField();
            _folderPathField.style.flexGrow = 1;
            _folderPathField.value = QuestMapQuery.GetScannedFolderPath();
            _folderPathField.isReadOnly = true;

            _browseFolderButton = new Button(BrowseFolder) { text = "Browse..." };
            _browseFolderButton.style.width = 80;
            _browseFolderButton.style.marginLeft = 4;

            _buildCacheButton = new Button(BuildCache) { text = "Scan Scenes" };
            _buildCacheButton.style.width = 100;
            _buildCacheButton.style.marginLeft = 4;

            folderRow.Add(_folderPathField);
            folderRow.Add(_browseFolderButton);
            folderRow.Add(_buildCacheButton);
            folderSection.Add(folderRow);

            root.Add(folderSection);
        }

        void CreateModeSelector(VisualElement root) {
            var modeContainer = new VisualElement();
            modeContainer.style.flexDirection = FlexDirection.Row;
            modeContainer.style.marginBottom = 8;
            modeContainer.style.marginTop = 4;

            _questModeButton = new Button(() => SetSearchMode(SearchMode.Quest)) { text = "Search Quest" };
            _questModeButton.style.flexGrow = 1;
            _questModeButton.style.height = 32;

            _npcModeButton = new Button(() => SetSearchMode(SearchMode.NPC)) { text = "Search NPC" };
            _npcModeButton.style.flexGrow = 1;
            _npcModeButton.style.height = 32;
            _npcModeButton.style.marginLeft = 4;

            _sceneModeButton = new Button(() => SetSearchMode(SearchMode.Scene)) { text = "Search Scene" };
            _sceneModeButton.style.flexGrow = 1;
            _sceneModeButton.style.height = 32;
            _sceneModeButton.style.marginLeft = 4;

            modeContainer.Add(_questModeButton);
            modeContainer.Add(_npcModeButton);
            modeContainer.Add(_sceneModeButton);

            root.Add(modeContainer);

            UpdateModeButtons();
        }

        void CreateSearchBar(VisualElement root) {
            var searchContainer = new VisualElement();
            searchContainer.style.flexDirection = FlexDirection.Row;
            searchContainer.style.marginBottom = 8;

            _searchField = new TextField();
            _searchField.style.flexGrow = 1;
            _searchField.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return) {
                    PerformSearch();
                }
            });
            UpdateSearchFieldPlaceholder();

            _searchButton = new Button(PerformSearch) { text = "Search" };
            _searchButton.style.width = 80;
            _searchButton.style.marginLeft = 4;

            _listAllButton = new Button(ShowAll) { text = "List All" };
            _listAllButton.style.width = 80;
            _listAllButton.style.marginLeft = 4;

            searchContainer.Add(_searchField);
            searchContainer.Add(_searchButton);
            searchContainer.Add(_listAllButton);

            root.Add(searchContainer);
        }

        // === Mode Management ===

        void SetSearchMode(SearchMode mode) {
            _currentMode = mode;
            UpdateModeButtons();
            UpdateSearchFieldPlaceholder();
            _resultsContainer.Clear();
            _currentSearchTerm = "";
            _searchField.value = "";

            string modeName = mode switch {
                SearchMode.Quest => "Quest",
                SearchMode.NPC => "NPC",
                SearchMode.Scene => "Scene",
                _ => "Unknown"
            };
            _statusLabel.text = $"Search mode: {modeName}";
        }

        void UpdateModeButtons() {
            _questModeButton.style.backgroundColor = _currentMode == SearchMode.Quest ? ActiveModeColor : InactiveModeColor;
            _npcModeButton.style.backgroundColor = _currentMode == SearchMode.NPC ? ActiveModeColor : InactiveModeColor;
            _sceneModeButton.style.backgroundColor = _currentMode == SearchMode.Scene ? ActiveModeColor : InactiveModeColor;
        }

        void UpdateSearchFieldPlaceholder() {
            _searchField.label = _currentMode switch {
                SearchMode.Quest => "Quest Name:",
                SearchMode.NPC => "NPC Name:",
                SearchMode.Scene => "Scene Name:",
                _ => "Search:"
            };
        }

        // === Actions ===

        void UpdateCacheInfo() {
            var (npcs, quests, stories, scenes, lastBuilt, folder) = QuestMapQuery.GetCacheStats();
            _cacheInfoLabel.text = $"Cache: {npcs} NPCs | {quests} Quests | {stories} Stories | {scenes} Scenes | Last: {lastBuilt}";
        }

        void BrowseFolder() {
            var currentPath = _folderPathField.value;
            if (string.IsNullOrEmpty(currentPath)) {
                currentPath = "Assets/";
            }

            var selectedPath = EditorUtility.OpenFolderPanel("Select Scenes Folder", currentPath, "");
            if (!string.IsNullOrEmpty(selectedPath)) {
                if (selectedPath.StartsWith(Application.dataPath)) {
                    selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                _folderPathField.value = selectedPath;
            }
        }

        void BuildCache() {
            var folderPath = _folderPathField.value;

            if (string.IsNullOrEmpty(folderPath)) {
                EditorUtility.DisplayDialog("Error", "Please select a folder first.", "OK");
                return;
            }

            _statusLabel.text = "Scanning... This may take a while.";
            _resultsContainer.Clear();

            EditorApplication.delayCall += () => {
                // TODO: Need to integrate with SceneCacheBaker instead of direct folder scanning
                Log.Important?.Warning("QuestMapTool: Direct folder scanning no longer supported. Use TG/Build/Baking/Bake Scene Cache instead.");
                _statusLabel.text = "Use TG/Build/Baking/Bake Scene Cache instead";
                
                QuestMapQuery.ClearCachedReference();
                UpdateCacheInfo();

                if (!string.IsNullOrEmpty(_currentSearchTerm)) {
                    PerformSearch();
                }
            };
        }

        void PerformSearch() {
            _currentSearchTerm = _searchField.value;

            if (string.IsNullOrEmpty(_currentSearchTerm)) {
                _statusLabel.text = "Enter a search term";
                _resultsContainer.Clear();
                return;
            }

            if (!QuestMapQuery.IsCacheValid()) {
                ShowNoCacheWarning();
                return;
            }

            _resultsContainer.Clear();

            switch (_currentMode) {
                case SearchMode.Quest:
                    SearchQuests(_currentSearchTerm);
                    break;
                case SearchMode.NPC:
                    SearchNPCs(_currentSearchTerm);
                    break;
                case SearchMode.Scene:
                    SearchScenes(_currentSearchTerm);
                    break;
            }
        }

        void ShowAll() {
            if (!QuestMapQuery.IsCacheValid()) {
                ShowNoCacheWarning();
                return;
            }

            _resultsContainer.Clear();
            _currentSearchTerm = "";
            _searchField.value = "";

            switch (_currentMode) {
                case SearchMode.Quest:
                    ShowAllQuests();
                    break;
                case SearchMode.NPC:
                    ShowAllNPCs();
                    break;
                case SearchMode.Scene:
                    ShowAllScenes();
                    break;
            }
        }

        void ShowNoCacheWarning() {
            _resultsContainer.Clear();
            var warning = new Label("⚠ No cache found. Select a scenes folder and click 'Scan Scenes' to build the cache.");
            warning.style.fontSize = 14;
            warning.style.color = new Color(1f, 0.8f, 0f);
            warning.style.marginTop = 20;
            warning.style.unityTextAlign = TextAnchor.MiddleCenter;
            _resultsContainer.Add(warning);
        }

        // === Quest Search Mode ===

        void SearchQuests(string searchTerm) {
            var results = QuestMapQuery.SearchQuestsByName(searchTerm);

            if (results.Count == 0) {
                _statusLabel.text = $"No quests found matching '{searchTerm}'";
                var noResults = new Label($"No quests found for '{searchTerm}'");
                noResults.style.marginTop = 20;
                noResults.style.unityTextAlign = TextAnchor.MiddleCenter;
                noResults.style.color = new Color(0.7f, 0.7f, 0.7f);
                _resultsContainer.Add(noResults);
                return;
            }

            _statusLabel.text = $"Found {results.Count} quest(s) matching '{searchTerm}'";

            foreach (var quest in results) {
                DisplayQuestResult(quest);
            }
        }

        void ShowAllQuests() {
            var allQuests = QuestMapQuery.GetAllQuests();
            _statusLabel.text = $"Showing all {allQuests.Count} quests";

            foreach (var quest in allQuests) {
                var line = CreateClickableLabel($"[{quest.questType}] {quest.name}", 11,
                    () => PingAsset(quest.assetPath));
                line.style.marginBottom = 2;
                _resultsContainer.Add(line);
            }
        }

        void DisplayQuestResult(QuestSearchEntry questEntry) {
            var questData = QuestMapQuery.GetQuestData(questEntry.guid);
            if (questData == null) {
                return;
            }

            var container = CreateResultContainer();

            // Header
            var header = new Label($"<b>[{questData.questType}] {questData.name}</b>");
            header.style.fontSize = 16;
            header.style.marginBottom = 4;
            container.Add(header);

            var guid = new Label($"GUID: {questData.guid}");
            guid.style.fontSize = 9;
            guid.style.color = new Color(0.5f, 0.5f, 0.5f);
            guid.style.marginBottom = 8;
            container.Add(guid);

            // Branching info
            if (questData.hasMultipleBranches) {
                var branchLabel = new Label("⚠ Quest has multiple objective branches");
                branchLabel.style.fontSize = 10;
                branchLabel.style.color = new Color(1f, 0.9f, 0.5f);
                branchLabel.style.marginBottom = 4;
                container.Add(branchLabel);
            }

            // NPCs
            var npcs = QuestMapQuery.GetNpcsForQuest(questData.guid);
            if (npcs.Count > 0) {
                var npcsHeader = new Label($"NPCs Involved ({npcs.Count}):");
                npcsHeader.style.fontSize = 12;
                npcsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                npcsHeader.style.marginTop = 8;
                npcsHeader.style.marginBottom = 4;
                container.Add(npcsHeader);

                foreach (var npc in npcs) {
                    var npcLabel = new Label($"  • {npc.name}");
                    npcLabel.style.fontSize = 11;
                    npcLabel.style.color = new Color(0.8f, 0.9f, 1f);
                    npcLabel.style.marginLeft = 12;
                    container.Add(npcLabel);
                }
            }

            // Objectives
            if (questData.objectives.Count > 0) {
                var objHeader = new Label($"Objectives ({questData.objectives.Count}):");
                objHeader.style.fontSize = 12;
                objHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                objHeader.style.marginTop = 8;
                objHeader.style.marginBottom = 4;
                container.Add(objHeader);

                for (int i = 0; i < questData.objectives.Count; i++) {
                    var obj = questData.objectives[i];
                    var objLabel = new Label($"  {i + 1}. {obj.name}");
                    objLabel.style.fontSize = 11;
                    objLabel.style.marginLeft = 12;
                    objLabel.style.marginBottom = 2;
                    container.Add(objLabel);

                    if (!string.IsNullOrEmpty(obj.sceneName)) {
                        var sceneLabel = new Label($"     Scene: {obj.sceneName}");
                        sceneLabel.style.fontSize = 10;
                        sceneLabel.style.color = new Color(0.7f, 0.8f, 1f);
                        sceneLabel.style.marginLeft = 16;
                        container.Add(sceneLabel);
                    }

                    if (obj.prerequisites.Count > 0) {
                        var flagLabel = new Label($"     Requires flags: {string.Join(", ", obj.prerequisites)}");
                        flagLabel.style.fontSize = 9;
                        flagLabel.style.color = new Color(1f, 0.9f, 0.6f);
                        flagLabel.style.marginLeft = 16;
                        container.Add(flagLabel);
                    }
                }
            }

            // Scenes
            var scenes = QuestMapQuery.GetScenesForQuest(questData.guid);
            if (scenes.Count > 0) {
                var scenesHeader = new Label($"Scenes ({scenes.Count}):");
                scenesHeader.style.fontSize = 12;
                scenesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                scenesHeader.style.marginTop = 8;
                scenesHeader.style.marginBottom = 4;
                container.Add(scenesHeader);

                foreach (var scene in scenes) {
                    var sceneRow = CreateClickableLabel($"  • {scene.sceneName}", 11,
                        () => PingAsset(scene.scenePath),
                        () => OpenScene(scene.scenePath));
                    sceneRow.style.marginLeft = 12;
                    container.Add(sceneRow);
                }
            }

            // Related Quests
            var relatedQuests = QuestMapQuery.GetRelatedQuests(questData.guid);
            if (relatedQuests.Count > 0) {
                var relatedHeader = new Label($"Related Quests (shared flags: {relatedQuests.Count}):");
                relatedHeader.style.fontSize = 12;
                relatedHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                relatedHeader.style.marginTop = 8;
                relatedHeader.style.marginBottom = 4;
                container.Add(relatedHeader);

                foreach (var relatedGuid in relatedQuests) {
                    var relatedData = QuestMapQuery.GetQuestData(relatedGuid);
                    if (relatedData != null) {
                        var relatedLabel = new Label($"  • [{relatedData.questType}] {relatedData.name}");
                        relatedLabel.style.fontSize = 10;
                        relatedLabel.style.color = new Color(1f, 0.9f, 0.7f);
                        relatedLabel.style.marginLeft = 12;
                        container.Add(relatedLabel);
                    }
                }
            }

            _resultsContainer.Add(container);
        }

        // === NPC Search Mode ===

        void SearchNPCs(string searchTerm) {
            var results = QuestMapQuery.SearchNpcsByName(searchTerm);

            if (results.Count == 0) {
                _statusLabel.text = $"No NPCs found matching '{searchTerm}'";
                var noResults = new Label($"No NPCs found for '{searchTerm}'");
                noResults.style.marginTop = 20;
                noResults.style.unityTextAlign = TextAnchor.MiddleCenter;
                noResults.style.color = new Color(0.7f, 0.7f, 0.7f);
                _resultsContainer.Add(noResults);
                return;
            }

            _statusLabel.text = $"Found {results.Count} NPC(s) matching '{searchTerm}'";

            foreach (var npc in results) {
                DisplayNpcResult(npc);
            }
        }

        void ShowAllNPCs() {
            var allNpcs = QuestMapQuery.GetAllNpcs();
            _statusLabel.text = $"Showing all {allNpcs.Count} NPCs";

            foreach (var npc in allNpcs) {
                var storyCount = QuestMapQuery.GetStoriesForNpc(npc.guid).Count;
                var questCount = QuestMapQuery.GetQuestsForNpc(npc.guid).Count;
                var sceneCount = QuestMapQuery.GetScenesForNpc(npc.guid).Count;

                var line = new Label($"{npc.name} - {storyCount} stories, {questCount} quests, {sceneCount} scenes");
                line.style.fontSize = 11;
                line.style.marginBottom = 2;
                _resultsContainer.Add(line);
            }
        }

        void DisplayNpcResult(NpcSearchEntry npc) {
            var container = CreateResultContainer();

            // Header
            var header = new Label($"<b>{npc.name}</b>");
            header.style.fontSize = 16;
            header.style.marginBottom = 4;
            container.Add(header);

            var guid = new Label($"GUID: {npc.guid}");
            guid.style.fontSize = 9;
            guid.style.color = new Color(0.5f, 0.5f, 0.5f);
            guid.style.marginBottom = 8;
            container.Add(guid);

            // First Encounter
            if (!string.IsNullOrEmpty(npc.firstStoryName)) {
                var firstEncounter = new Label($"⭐ First Encounter: {npc.firstStoryName}");
                firstEncounter.style.fontSize = 12;
                firstEncounter.style.color = new Color(1f, 1f, 0.7f);
                firstEncounter.style.unityFontStyleAndWeight = FontStyle.Bold;
                firstEncounter.style.marginBottom = 8;
                container.Add(firstEncounter);
            }

            // Stories
            var stories = QuestMapQuery.GetStoriesForNpc(npc.guid);
            if (stories.Count > 0) {
                var storiesHeader = new Label($"Stories ({stories.Count}):");
                storiesHeader.style.fontSize = 12;
                storiesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                storiesHeader.style.marginTop = 4;
                storiesHeader.style.marginBottom = 4;
                container.Add(storiesHeader);

                foreach (var story in stories) {
                    var storyRow = CreateClickableLabel($"  • {story.storyName}", 11,
                        () => PingAsset(story.storyPath));
                    storyRow.style.marginLeft = 12;
                    container.Add(storyRow);
                }
            }

            // Quests (NPC Journey)
            var quests = QuestMapQuery.GetQuestsForNpc(npc.guid);
            if (quests.Count > 0) {
                var questsHeader = new Label($"Quest Journey ({quests.Count}):");
                questsHeader.style.fontSize = 12;
                questsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                questsHeader.style.marginTop = 8;
                questsHeader.style.marginBottom = 4;
                container.Add(questsHeader);

                foreach (var quest in quests) {
                    var questLabel = new Label($"  • [{quest.questType}] {quest.questName}");
                    questLabel.style.fontSize = 11;
                    questLabel.style.marginLeft = 12;
                    container.Add(questLabel);
                }
            }

            // Scenes
            var scenes = QuestMapQuery.GetScenesForNpc(npc.guid);
            if (scenes.Count > 0) {
                var scenesHeader = new Label($"Scenes ({scenes.Count}):");
                scenesHeader.style.fontSize = 12;
                scenesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                scenesHeader.style.marginTop = 8;
                scenesHeader.style.marginBottom = 4;
                container.Add(scenesHeader);

                foreach (var scene in scenes) {
                    var sceneContainer = new VisualElement();
                    sceneContainer.style.marginLeft = 12;
                    sceneContainer.style.marginBottom = 4;

                    var sceneRow = CreateClickableLabel($"  • {scene.sceneName}", 11,
                        () => PingAsset(scene.scenePath),
                        () => OpenScene(scene.scenePath));
                    sceneContainer.Add(sceneRow);

                    if (scene.isManual) {
                        var manualLabel = new Label("    [Manual - Story Controlled]");
                        manualLabel.style.fontSize = 9;
                        manualLabel.style.color = new Color(0.8f, 0.8f, 1f);
                        sceneContainer.Add(manualLabel);
                    } else if (!string.IsNullOrEmpty(scene.flagCondition)) {
                        var flagLabel = new Label($"    [Flag: {scene.flagCondition}]");
                        flagLabel.style.fontSize = 9;
                        flagLabel.style.color = new Color(1f, 0.9f, 0.6f);
                        sceneContainer.Add(flagLabel);
                    }

                    container.Add(sceneContainer);
                }
            } else {
                var noScenes = new Label("  No physical presence in scanned scenes");
                noScenes.style.fontSize = 11;
                noScenes.style.color = new Color(0.7f, 0.7f, 0.7f);
                noScenes.style.marginTop = 8;
                container.Add(noScenes);
            }

            _resultsContainer.Add(container);
        }

        // === Scene Search Mode ===

        void SearchScenes(string searchTerm) {
            var results = QuestMapQuery.SearchScenesByName(searchTerm);

            if (results.Count == 0) {
                _statusLabel.text = $"No scenes found matching '{searchTerm}'";
                var noResults = new Label($"No scenes found for '{searchTerm}'");
                noResults.style.marginTop = 20;
                noResults.style.unityTextAlign = TextAnchor.MiddleCenter;
                noResults.style.color = new Color(0.7f, 0.7f, 0.7f);
                _resultsContainer.Add(noResults);
                return;
            }

            _statusLabel.text = $"Found {results.Count} scene(s) matching '{searchTerm}'";

            foreach (var scene in results) {
                DisplaySceneResult(scene);
            }
        }

        void ShowAllScenes() {
            var allScenes = QuestMapQuery.GetAllScenes();
            _statusLabel.text = $"Showing all {allScenes.Count} scenes";

            foreach (var scene in allScenes) {
                var line = CreateClickableLabel($"{scene.sceneName} - {scene.npcCount} NPC(s)", 11,
                    () => PingAsset(scene.scenePath));
                line.style.marginBottom = 2;
                _resultsContainer.Add(line);
            }
        }

        void DisplaySceneResult(SceneSearchEntry sceneEntry) {
            var npcs = QuestMapQuery.GetNpcsForScene(sceneEntry.scenePath);

            var container = CreateResultContainer();

            // Header
            var header = CreateClickableLabel($"<b>{sceneEntry.sceneName}</b>", 16,
                () => PingAsset(sceneEntry.scenePath),
                () => OpenScene(sceneEntry.scenePath));
            header.style.marginBottom = 4;
            container.Add(header);

            var path = new Label($"Path: {sceneEntry.scenePath}");
            path.style.fontSize = 9;
            path.style.color = new Color(0.5f, 0.5f, 0.5f);
            path.style.marginBottom = 8;
            container.Add(path);

            // NPCs
            if (npcs.Count > 0) {
                var npcsHeader = new Label($"NPCs in Scene ({npcs.Count}):");
                npcsHeader.style.fontSize = 12;
                npcsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                npcsHeader.style.marginTop = 4;
                npcsHeader.style.marginBottom = 4;
                container.Add(npcsHeader);

                foreach (var npc in npcs) {
                    var npcContainer = new VisualElement();
                    npcContainer.style.marginLeft = 12;
                    npcContainer.style.marginBottom = 6;

                    var npcLabel = new Label($"  • {npc.npcName}");
                    npcLabel.style.fontSize = 11;
                    npcLabel.style.color = new Color(0.8f, 0.9f, 1f);
                    npcContainer.Add(npcLabel);

                    // Story start point (for QA)
                    if (!string.IsNullOrEmpty(npc.firstStoryName)) {
                        var storyStart = new Label($"    Story Start: {npc.firstStoryName}");
                        storyStart.style.fontSize = 9;
                        storyStart.style.color = new Color(0.7f, 1f, 0.7f);
                        npcContainer.Add(storyStart);
                    } else {
                        var noStory = new Label("    Story Start: Unknown (not in any story)");
                        noStory.style.fontSize = 9;
                        noStory.style.color = new Color(1f, 0.7f, 0.7f);
                        npcContainer.Add(noStory);
                    }

                    // Presence type
                    if (npc.isManual) {
                        var manualLabel = new Label("    [Manual - Story Controlled]");
                        manualLabel.style.fontSize = 9;
                        manualLabel.style.color = new Color(0.8f, 0.8f, 1f);
                        npcContainer.Add(manualLabel);
                    } else if (!string.IsNullOrEmpty(npc.flagCondition)) {
                        var flagLabel = new Label($"    [Flag: {npc.flagCondition}]");
                        flagLabel.style.fontSize = 9;
                        flagLabel.style.color = new Color(1f, 0.9f, 0.6f);
                        npcContainer.Add(flagLabel);
                    } else {
                        var alwaysLabel = new Label("    [Always Present]");
                        alwaysLabel.style.fontSize = 9;
                        alwaysLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                        npcContainer.Add(alwaysLabel);
                    }

                    container.Add(npcContainer);
                }
            } else {
                var noNpcs = new Label("  No NPCs found in this scene");
                noNpcs.style.fontSize = 11;
                noNpcs.style.color = new Color(0.7f, 0.7f, 0.7f);
                noNpcs.style.marginTop = 8;
                container.Add(noNpcs);
            }

            _resultsContainer.Add(container);
        }

        // === UI Helpers ===

        VisualElement CreateResultContainer() {
            var container = new VisualElement();
            container.style.marginBottom = 16;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;
            return container;
        }

        Label CreateClickableLabel(string text, int fontSize, System.Action onLeftClick, System.Action onDoubleClick = null) {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = new Color(0.7f, 0.9f, 1f);

            label.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button == 0) {
                    if (evt.clickCount == 2 && onDoubleClick != null) {
                        onDoubleClick();
                    } else if (evt.clickCount == 1) {
                        onLeftClick();
                    }
                }
            });

            return label;
        }

        void PingAsset(string path) {
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null) {
                EditorGUIUtility.PingObject(asset);
                Log.Debug?.Info($"Pinged asset: {path}");
            }
        }

        void OpenScene(string path) {
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                Log.Debug?.Info($"Opened scene: {path}");
            }
        }
    }
}
