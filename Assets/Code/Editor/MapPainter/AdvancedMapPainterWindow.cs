using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using Awaken.ECS.MedusaRenderer;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.Previews;
using Awaken.Utility.LowLevel.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.MapPainter {
    public class AdvancedMapPainterWindow : EditorWindow {
        const int PrefabThumbnailSize = 64;
        const int PrefabRemoveButtonHeight = 20;
        const int PrefabFullHeight = PrefabThumbnailSize + PrefabRemoveButtonHeight;
        const byte HitsPerRaycast = 8;

        [SerializeField] MapPainterProfile currentProfile;

        // Working variables (synced with currentProfile)
        [SerializeField] List<GameObject> selectedPrefabs = new List<GameObject>();
        [SerializeField] List<PrefabSettings> prefabSettingsList = new List<PrefabSettings>();
        [SerializeField] int selectedPrefabIndex = -1;

        // Enhanced brush settings (current active settings)
        [SerializeField] float brushSize = 5f;
        [SerializeField] int maxDensity = 20;
        [SerializeField] float spawnRate = 0.3f;
        [SerializeField] float minSpawnDistance = 0.5f;
        [SerializeField] float trimRate = 0.3f;
        [SerializeField] bool randomRotation = true;
        [SerializeField] bool randomScale = false;
        [SerializeField] Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        [SerializeField] MapPainterUtility.DistributionPattern distributionPattern = MapPainterUtility.DistributionPattern.Random;

        // Slope and height filtering
        [SerializeField] bool useSlopeFilter = false;
        [SerializeField] Vector2 slopeRange = new Vector2(0f, 30f);
        [SerializeField] bool useHeightFilter = false;
        [SerializeField] Vector2 heightRange = new Vector2(0f, 100f);
        [SerializeField] bool canPaintByDrag;

        // Organization settings
        [SerializeField] bool useParentGroups = true;
        [SerializeField] string parentGroupName = "Painted Objects";
        [SerializeField] bool useManualGroups = true;
        [SerializeField] bool showAllGroups = false;

        // Manual group management
        [SerializeField] List<GameObject> manualGroups = new List<GameObject>();
        [SerializeField] int selectedGroupIndex = -1;
        [SerializeField] string newGroupName = "New Group";
        Transform _rootGroupParent;

        RaycastHit[] _previewHitsCache = new RaycastHit[128];

        // UI state
        Vector2 _prefabScrollPosition;
        Vector2 _settingsScrollPosition;
        bool _isPainting = false;
        bool _showAdvancedSettings = false;
        bool _paintModeEnabled = false;
        Camera _sceneCamera;

        GUIContent _guiContent;
        GUIStyle _titleStyle;
        GUIStyle _sceneHeaderStyle;

        GUIStyle TitleStyle {
            get {
                if (_titleStyle == null) {
                    _titleStyle = new GUIStyle(EditorStyles.boldLabel) {
                        fontSize = 16,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                return _titleStyle;
            }
        }

        GUIStyle SceneHeaderStyle {
            get {
                if (_sceneHeaderStyle == null) {
                    _sceneHeaderStyle = new GUIStyle(EditorStyles.helpBox) {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 18,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Color.green }
                    };
                }
                return _sceneHeaderStyle;
            }
        }

        bool ShowInstructions {
            get => EditorPrefs.GetBool("MapPainter_ShowInstructions", true);
            set => EditorPrefs.SetBool("MapPainter_ShowInstructions", value);
        }

        [MenuItem("ArtTools/Map Painter")]
        public static void ShowWindow() {
            GetWindow<AdvancedMapPainterWindow>("Map Painter");
        }

        void OnEnable() {
            _guiContent = new GUIContent();
            EditorApplication.playModeStateChanged += OnEditorApplicationOnplayModeStateChanged;
            if (!Application.isPlaying) {
                SceneView.duringSceneGui += OnSceneGUI;
            }
            LoadLastProfile();
        }

        void OnDisable() {
            EditorApplication.playModeStateChanged -= OnEditorApplicationOnplayModeStateChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            SaveProfilePreference();
        }

        void OnGUI() {
            EditorGUILayout.LabelField("Advanced Map Painter Tool", TitleStyle);

            DrawProfileManagement();
            EditorGUILayout.Space();

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.P && e.modifiers == EventModifiers.None) {
                _paintModeEnabled = !_paintModeEnabled;
                SceneView.RepaintAll();
                e.Use();
                Repaint();
                return;
            }

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = _paintModeEnabled ? Color.green : Color.red;

            if (GUILayout.Button(_paintModeEnabled ? "PAINT MODE: ON (Press P)" : "PAINT MODE: OFF (Press P)", GUILayout.Height(30))) {
                _paintModeEnabled = !_paintModeEnabled;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = originalColor;

            if (!_paintModeEnabled) {
                EditorGUILayout.HelpBox("Paint Mode is OFF. Enable it to start painting. Normal Unity scene editing is active.", MessageType.Info);
            } else {
                EditorGUILayout.HelpBox("Paint Mode is ON. Scene painting is active. Click the button above to return to normal editing.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            _settingsScrollPosition = EditorGUILayout.BeginScrollView(_settingsScrollPosition);

            DrawPrefabSelection();
            EditorGUILayout.Space();
            DrawBrushSettings();
            EditorGUILayout.Space();
            DrawAdvancedSettings();
            EditorGUILayout.Space();
            DrawManagementTools();
            EditorGUILayout.Space();
            DrawInstructions();

            EditorGUILayout.EndScrollView();
        }

        void OnSceneGUI(SceneView sceneView) {
            var e = Event.current;

            if (focusedWindow != this) {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.P && e.modifiers == EventModifiers.None) {
                    _paintModeEnabled = !_paintModeEnabled;
                    SceneView.RepaintAll();
                    Repaint();
                    e.Use();
                    return;
                }
            }

            DrawSceneGui(sceneView);

            if (!_paintModeEnabled || selectedPrefabs.Count == 0) {
                return;
            }

            if (e.type == EventType.MouseUp) {
                _isPainting = false;
                e.Use();
            }
            if (e.type == EventType.ScrollWheel && e.alt) {
                brushSize = Mathf.Clamp(brushSize + e.delta.y * 0.5f, 0.5f, 50f);
                e.Use();
            }

            var mousePos = e.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mousePos);

            var hit = default(RaycastHit);
            bool hitSomething = false;

            var size = Physics.RaycastNonAlloc(ray, _previewHitsCache, Mathf.Infinity);
            for (var i = 0; i < size; i++) {
                var testHit = _previewHitsCache[i];
                if (IsSurfacePaintable(testHit)) {
                    if (hitSomething)
                    {
                        if (testHit.distance < hit.distance)
                        {
                            hit = testHit;
                        }
                    }
                    else
                    {
                        hit = testHit;
                        hitSomething = true;
                    }
                }
            }

            if (!hitSomething) {
                return;
            }

            DrawBrushPreview(hit.point, hit.normal);

            if (e.type == EventType.MouseDown && e.button == 0) {
                // Let Unity handle camera rotation
                if (e.alt && !e.shift) {
                    return;
                }

                // Single click spawning
                if (e.shift && e.alt) {
                    TrimPrefabs(hit.point);
                } else if (e.shift && e.control) {
                    EraseAllPrefabs(hit.point);
                } else if (e.shift) {
                    EraseSelectedPrefab(hit.point);
                } else {
                    PaintPrefabs(hit.point, hit.normal);
                }
                _isPainting = true;
                e.Use();
            } else if (e.type == EventType.MouseDrag && e.button == 0 && _isPainting) {
                // Don't paint if Alt is held (camera rotation shortcut)
                if (e.alt && !e.shift) {
                    _isPainting = false;
                    return;
                }

                if (canPaintByDrag) {
                    // Continuous painting while dragging
                    if (e.shift && e.alt) {
                        TrimPrefabs(hit.point);
                    } else if (e.shift && e.control) {
                        EraseAllPrefabs(hit.point);
                    } else if (e.shift) {
                        EraseSelectedPrefab(hit.point);
                    } else {
                        PaintPrefabs(hit.point, hit.normal);
                    }
                }
                e.Use();
            }
        }

        void DrawProfileManagement() {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Profile Management", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            currentProfile = EditorGUILayout.ObjectField("Current Profile", currentProfile, typeof(MapPainterProfile), false) as MapPainterProfile;
            if (EditorGUI.EndChangeCheck()) {
                LoadFromProfile();
            }

            if (GUILayout.Button("New", GUILayout.Width(50))) {
                CreateNewProfile();
            }

            if (GUILayout.Button("Save", GUILayout.Width(50))) {
                SaveToProfile();
            }

            EditorGUILayout.EndHorizontal();

            if (currentProfile == null) {
                EditorGUILayout.HelpBox("No profile selected. Create a new profile or select an existing one to get started.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        void DrawPrefabSelection() {
            var mainArea = EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Prefab Selection", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Prefabs", GUILayout.Width(100))) {
                AddPrefabsFromSelection();
            }
            if (GUILayout.Button("Add From Folder", GUILayout.Width(120))) {
                AddPrefabsFromFolder();
            }
            if (GUILayout.Button("Refresh Thumbnails", GUILayout.Width(130))) {
                RefreshThumbnails();
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(80))) {
                selectedPrefabs.Clear();
                prefabSettingsList.Clear();
                selectedPrefabIndex = -1;
            }
            EditorGUILayout.EndHorizontal();

            HandleDragAndDrop(mainArea);

            if (selectedPrefabs.Count > 0) {
                EditorGUILayout.LabelField($"Prefabs ({selectedPrefabs.Count}):");

                var availableWidth = position.width - 25 - (EditorGUIUtility.standardVerticalSpacing * 2);
                int columns = Mathf.FloorToInt(availableWidth / (PrefabThumbnailSize + EditorGUIUtility.standardVerticalSpacing));
                int rows = Mathf.CeilToInt((float) selectedPrefabs.Count / columns);

                var lineHeight = EditorGUIUtility.standardVerticalSpacing;
                var rowHeight = PrefabFullHeight + lineHeight * 2;
                _prefabScrollPosition = EditorGUILayout.BeginScrollView(_prefabScrollPosition, GUILayout.Height(rowHeight));

                GUILayoutUtility.GetRect(availableWidth, rows * rowHeight);

                var deleteMask = new UnsafeBitmask((uint)selectedPrefabs.Count, ARAlloc.Temp);
                for (int i = 0; i < selectedPrefabs.Count; i++) {
                    var column = i % columns;
                    var row = i / columns;

                    DrawPrefabThumbnail(i, column, row, ref deleteMask);
                }

                EditorGUILayout.EndScrollView();

                for (var i = selectedPrefabs.Count - 1; i >= 0; i--) {
                    if (deleteMask[(uint)i]) {
                        RemovePrefabAt(i);
                    }
                }

                deleteMask.Dispose();

                if (selectedPrefabIndex != -1) {
                    EditorGUILayout.LabelField($"Selected: {selectedPrefabs[selectedPrefabIndex].name}");
                }
            } else {
                EditorGUILayout.HelpBox("No prefabs selected. Drag prefabs here, select prefabs in Project window and click 'Add Prefabs', or use 'Add From Folder'.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawPrefabThumbnail(int index, int column, int row, ref UnsafeBitmask deleteMask) {
            var prefab = selectedPrefabs[index];

            if (prefab == null) {
                deleteMask.Up((uint)index);
            }

            var preview = Texture2D.whiteTexture;// prefab ? AssetPreviewCache.GetCachedAssetPreview(prefab) : Texture2D.whiteTexture;
            var prefabName = prefab ? prefab.name : "Missing Prefab";

            var originalColor = GUI.backgroundColor;
            if (selectedPrefabIndex == index) {
                GUI.backgroundColor = Color.cyan;
            }

            var rect = new Rect(0, 0, PrefabThumbnailSize, PrefabFullHeight);
            rect.x += EditorGUIUtility.standardVerticalSpacing + (PrefabThumbnailSize + EditorGUIUtility.standardVerticalSpacing) * column;
            rect.y += row * (PrefabFullHeight + EditorGUIUtility.standardVerticalSpacing);

            _guiContent = new GUIContent(preview ? preview : EditorGUIUtility.whiteTexture, prefabName);

            rect.height = PrefabThumbnailSize;
            rect.width = PrefabThumbnailSize;

            if (GUI.Button(rect, _guiContent)) {
                SaveCurrentSettings();
                selectedPrefabIndex = index;
                LoadSettingsForCurrentPrefab();
            }

            rect.y += PrefabThumbnailSize;
            rect.height = PrefabRemoveButtonHeight;

            if (GUI.Button(rect, "×")) {
                deleteMask.Up((uint)index);
            }

            GUI.backgroundColor = originalColor;
        }

        void DrawBrushSettings() {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.5f, 50f);
            maxDensity = EditorGUILayout.IntSlider("Max Density", maxDensity, 1, 50);
            spawnRate = EditorGUILayout.Slider("Spawn Rate", spawnRate, 0.01f, 0.6f);
            trimRate = EditorGUILayout.Slider("Trim Rate", trimRate, 0.1f, 1.0f);
            minSpawnDistance = EditorGUILayout.Slider("Min Spawn Distance", minSpawnDistance, 0.1f, 5f);

            EditorGUILayout.Space();
            distributionPattern = (MapPainterUtility.DistributionPattern)EditorGUILayout.EnumPopup("Distribution Pattern", distributionPattern);

            EditorGUILayout.Space();
            randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);
            randomScale = EditorGUILayout.Toggle("Random Scale", randomScale);

            if (randomScale) {
                scaleRange = EditorGUILayout.Vector2Field("Scale Range", scaleRange);
            }

            if (EditorGUI.EndChangeCheck()) {
                SaveCurrentSettings();
            }

            EditorGUILayout.Space();
            int baseDensity = CalculateBaseDensity();
            int actualDensity = CalculateSpawnCount();
            EditorGUILayout.HelpBox($"Base density: {baseDensity} → Actual spawn: {actualDensity} objects per brush stroke", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        void DrawAdvancedSettings() {
            EditorGUILayout.BeginVertical("box");

            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true);

            if (!_showAdvancedSettings) {
                ValidateGroups();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Fixed layers: Terrain (always paintable) and Walkable (paintable if has MedusaRendererPrefab)", MessageType.Info);

            canPaintByDrag = EditorGUILayout.Toggle("Use Drag", canPaintByDrag);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Filtering", EditorStyles.boldLabel);

            useSlopeFilter = EditorGUILayout.Toggle("Use Slope Filter", useSlopeFilter);
            if (useSlopeFilter) {
                slopeRange = EditorGUILayout.Vector2Field("Slope Range (degrees)", slopeRange);
            }

            useHeightFilter = EditorGUILayout.Toggle("Use Height Filter", useHeightFilter);
            if (useHeightFilter) {
                heightRange = EditorGUILayout.Vector2Field("Height Range", heightRange);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Organization", EditorStyles.boldLabel);

            useParentGroups = EditorGUILayout.Toggle("Use Parent Groups", useParentGroups);
            if (useParentGroups) {
                parentGroupName = EditorGUILayout.TextField("Parent Group Name", parentGroupName);

                EditorGUILayout.Space();
                useManualGroups = EditorGUILayout.Toggle("Use Manual Groups", useManualGroups);

                if (useManualGroups) {
                    EditorGUI.indentLevel++;

                    EditorGUI.BeginChangeCheck();
                    showAllGroups = EditorGUILayout.Toggle("Show All Groups", showAllGroups);
                    if (EditorGUI.EndChangeCheck()) {
                        UpdateAllGroupsVisibility();
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    newGroupName = EditorGUILayout.TextField("Group Name:", newGroupName);
                    if (GUILayout.Button("Add New Group", GUILayout.Width(120))) {
                        AddNewGroup();
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    DrawGroups();

                    EditorGUILayout.Space();

                    EditorGUILayout.HelpBox("Manual Groups let you organize painted objects into separate groups. " +
                                            "Only the active group is visible during painting for better performance.",
                        MessageType.Info);

                    EditorGUI.indentLevel--;
                } else {
                    ValidateGroups();
                }
            } else {
                ValidateGroups();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        void DrawGroups() {
            if (manualGroups.Count == 0) {
                EditorGUILayout.HelpBox("No groups created. Click 'Add New Group' to create your first group.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Groups:", EditorStyles.boldLabel);

            var groupsToDelete = new UnsafeBitmask((uint)manualGroups.Count, ARAlloc.Temp);

            for (int i = 0; i < manualGroups.Count; i++) {
                if (manualGroups[i] == null) {
                    groupsToDelete.Up((uint)i);
                    continue;
                }

                EditorGUILayout.BeginHorizontal();

                bool isSelected = selectedGroupIndex == i;
                Color originalColor = GUI.backgroundColor;
                if (isSelected) {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(isSelected ? "● Active" : "○", GUILayout.Width(60))) {
                    SelectGroup(i);
                }

                GUI.backgroundColor = originalColor;

                string oldName = manualGroups[i].name;
                string newName = EditorGUILayout.DelayedTextField(oldName);
                if (newName != oldName) {
                    Undo.RecordObject(manualGroups[i], "Rename Group");
                    manualGroups[i].name = newName;
                }

                if (GUILayout.Button("Delete", GUILayout.Width(60))) {
                    groupsToDelete.Up((uint)i);
                }

                EditorGUILayout.EndHorizontal();
            }

            var anyToDelete = groupsToDelete.AnySet();
            for (var i = manualGroups.Count-1; i >= 0; i--) {
                if (groupsToDelete[(uint)i]) {
                    DeleteGroup(i);
                }
            }

            if (anyToDelete) {
                UpdateAllGroupsVisibility();
            }

            groupsToDelete.Dispose();
        }

        void ValidateGroups() {
            var groupsToDelete = new UnsafeBitmask((uint)manualGroups.Count, ARAlloc.Temp);

            for (int i = 0; i < manualGroups.Count; i++) {
                if (manualGroups[i] == null) {
                    groupsToDelete.Up((uint)i);
                }
            }

            var anyToDelete = groupsToDelete.AnySet();
            for (var i = manualGroups.Count-1; i >= 0; i--) {
                if (groupsToDelete[(uint)i]) {
                    DeleteGroup(i);
                }
            }

            if (anyToDelete) {
                UpdateAllGroupsVisibility();
            }

            groupsToDelete.Dispose();
        }

        void DrawManagementTools() {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Management Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Count Painted Objects")) {
                CountPaintedObjects();
            }
            if (GUILayout.Button("Select All Painted")) {
                SelectAllPaintedObjects();
            }
            if (GUILayout.Button("Clear All Painted", GUILayout.Width(120))) {
                if (EditorUtility.DisplayDialog("Clear All Painted Objects",
                    "Are you sure you want to delete ALL objects painted with this tool? This cannot be undone!",
                    "Yes, Delete All", "Cancel")) {
                    ClearAllPaintedObjects();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Config", GUILayout.Width(100))) {
                if (EditorUtility.DisplayDialog("Reset Configuration",
                    "Are you sure you want to reset all Map Painter settings? This will clear all prefabs and settings.",
                    "Yes, Reset", "Cancel")) {
                    ResetConfiguration();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        void DrawInstructions() {
            ShowInstructions = EditorGUILayout.Foldout(ShowInstructions, "Instructions", true);
            if (!ShowInstructions) {
                return;
            }
            EditorGUILayout.HelpBox(
                "• ENABLE PAINT MODE to start painting (button above)\n" +
                "• When Paint Mode is OFF, normal Unity scene editing works\n" +
                "• Drag & drop prefabs or use 'Add Prefabs'/'Add From Folder'\n" +
                "• Single Click in Scene view to spawn objects\n" +
                "• Hold and Drag Left Mouse Button to paint continuously\n" +
                "• Hold Shift + Click/Drag to erase selected prefab type only\n" +
                "• Hold Shift + Ctrl + Click/Drag to erase all painted objects\n" +
                "• Hold Shift + Alt + Click/Drag to trim selected prefab type (randomly delete some)\n" +
                "• Alt + Scroll wheel to adjust brush size\n" +
                "• Density automatically scales with brush size (limited by Max Density)\n" +
                "• Smart surface detection: Terrain layer (always), Walkable layer (only if has MedusaRendererPrefab)\n" +
                "• Painted objects are organized in manual groups\n" +
                "• Manual Groups: Create separate groups for different areas/purposes\n" +
                "• Only active group visible during painting (toggle 'Show All Groups' to see all)",
                MessageType.Info);
        }

        void AddPrefabsFromSelection() {
            Object[] selection = Selection.objects;
            foreach (Object obj in selection) {
                if (obj is GameObject prefab) {
                    TryAddGameObjectPrefab(prefab);
                }
            }
        }

        void AddPrefabsFromFolder() {
            var folderPath = EditorUtility.OpenFolderPanel("Select Folder with Prefabs", "Assets", "");
            if (!string.IsNullOrEmpty(folderPath)) {
                var relativePath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                TryAddPrefabsFromFolder(relativePath);
            }
        }

        void HandleDragAndDrop(Rect parentArea) {
            EditorGUILayout.HelpBox("Drag & Drop Prefabs Here", MessageType.Info);

            Event currentEvent = Event.current;

            if (!parentArea.Contains(currentEvent.mousePosition)) {
                return;
            }

            if (currentEvent.type == EventType.DragUpdated) {
                var hasAnyValidPrefab = false;
                var objectReferences = DragAndDrop.objectReferences;
                for (var i = 0; !hasAnyValidPrefab && i < objectReferences.Length; i++) {
                    var draggedObject = objectReferences[i];
                    if (draggedObject is GameObject draggedPrefab && IsValidPrefabToAdd(draggedPrefab)) {
                        hasAnyValidPrefab = true;
                    } else if (draggedObject is DefaultAsset) {
                        var path = AssetDatabase.GetAssetPath(draggedObject);
                        if (string.IsNullOrWhiteSpace(path) == false) {
                            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                            for (int j = 0; !hasAnyValidPrefab && j < guids.Length; j++) {
                                var guid = guids[j];
                                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                                var prefab = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
                                if (IsValidPrefabToAdd(prefab)) {
                                    hasAnyValidPrefab = true;
                                }
                            }
                        }
                    }
                }

                if (hasAnyValidPrefab) {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                } else {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }

                currentEvent.Use();
            } else if (currentEvent.type == EventType.DragPerform) {
                var objectReferences = DragAndDrop.objectReferences;
                for (var i = 0; i < objectReferences.Length; i++) {
                    var draggedObject = objectReferences[i];
                    if (draggedObject is GameObject draggedPrefab) {
                        TryAddGameObjectPrefab(draggedPrefab);
                    } else if (draggedObject is DefaultAsset) {
                        var path = AssetDatabase.GetAssetPath(draggedObject);
                        TryAddPrefabsFromFolder(path);
                    }
                }

                currentEvent.Use();
            }
        }

        void TryAddPrefabsFromFolder(string folderPath) {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (string guid in guids) {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;

                TryAddGameObjectPrefab(prefab);
            }
        }

        void TryAddGameObjectPrefab(GameObject prefab) {
            if (IsValidPrefabToAdd(prefab)) {
                selectedPrefabs.Add(prefab);
                prefabSettingsList.Add(new PrefabSettings());
            }
        }

        bool IsValidPrefabToAdd(GameObject prefab) {
            if (!prefab) {
                return false;
            }
            var prefabType = PrefabUtility.GetPrefabAssetType(prefab);
            var isValidPrefabType = prefabType is PrefabAssetType.Regular or PrefabAssetType.Variant;
            return isValidPrefabType && !selectedPrefabs.Contains(prefab);
        }

        void DrawSceneGui(SceneView sceneView) {
            if (!_paintModeEnabled) {
                return;
            }
            Handles.BeginGUI();
            var sceneRect = new Rect(0, 0, sceneView.position.width, sceneView.position.height);
            var borderColor = Color.red;
            var borderThickness = 2f;
            EditorGUI.DrawRect(new Rect(sceneRect.x, sceneRect.y, sceneRect.width, borderThickness), borderColor);
            EditorGUI.DrawRect(new Rect(sceneRect.x, sceneRect.height - borderThickness - 25, sceneRect.width, borderThickness), borderColor);
            EditorGUI.DrawRect(new Rect(sceneRect.x, sceneRect.y, borderThickness, sceneRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(sceneRect.width - borderThickness, sceneRect.y, borderThickness, sceneRect.height), borderColor);

            var boxWidth = 250f;
            var boxHeight = 32f;
            var boxRect = new Rect((sceneRect.width - boxWidth) / 2f, 10, boxWidth, boxHeight);
            GUI.backgroundColor = Color.white;
            GUI.Box(boxRect, "PAINT MODE ACTIVE", SceneHeaderStyle);
            GUI.backgroundColor = Color.green;

            Handles.EndGUI();
        }

        void DrawBrushPreview(Vector3 center, Vector3 normal) {
            Handles.color = _isPainting ? Color.red : Color.white;
            Handles.DrawWireDisc(center, normal, brushSize);

            // Show semi-transparent brush area
            Handles.color = new Color(1, 1, 1, 0.2f);
            Handles.DrawSolidDisc(center, normal, brushSize);
        }

        void PaintPrefabs(Vector3 center, Vector3 normal) {
            if (selectedPrefabIndex == -1) {
                return;
            }

            var prefab = selectedPrefabs[selectedPrefabIndex];
            var spawnParent = GetCurrentSelectedGroup();
            var spawnCount = CalculateSpawnCount();

            var spawnPositions = new NativeList<float3>(spawnCount, ARAlloc.TempJob);
            new MapPainterUtility.SpawnPointsGeneratorJob {
                center = center,
                radius = brushSize,
                count = spawnCount,
                pattern = distributionPattern,
                random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue)),
                outSpawnPoints = spawnPositions
            }.Run();

            if (spawnPositions.Length > 0) {
                // Group all spawns in one undo operation for better performance
                int undoGroupIndex = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Paint Prefabs Batch");

                PaintPrefabsBatched(spawnPositions, prefab, spawnParent, center);

                Undo.CollapseUndoOperations(undoGroupIndex);
            }

            spawnPositions.Dispose();
        }

        void PaintPrefabsBatched(NativeList<float3> spawnPositions, GameObject prefab, Transform parentGroup, Vector3 center) {
            // Prepare batch raycast commands
            var commands = new NativeArray<RaycastCommand>(spawnPositions.Length, Allocator.TempJob);
            var results = new NativeArray<RaycastHit>(spawnPositions.Length * HitsPerRaycast, Allocator.TempJob);
            var validIndices = new NativeList<int>(spawnPositions.Length, Allocator.TempJob);

            // Setup raycast commands
            var queryParams = new QueryParameters {
                hitTriggers = QueryTriggerInteraction.Ignore,
                layerMask = RenderLayers.Mask.Terrain | RenderLayers.Mask.Walkable
            };
            for (int i = 0; i < spawnPositions.Length; i++) {
                Vector3 rayStart = spawnPositions[i] + math.up() * 100f;
                commands[i] = new RaycastCommand(rayStart, Vector3.down, queryParams, 200f);
            }

            var commandsPerJob = math.max(spawnPositions.Length / JobsUtility.JobWorkerCount, 1);
            RaycastCommand.ScheduleBatch(commands, results, commandsPerJob, HitsPerRaycast).Complete();

            new FilterValidPositionsJob {
                raycastResults = results,
                maxHits = HitsPerRaycast,
                brushCenter = center,
                brushSizeSq = math.square(brushSize),
                minSpawnDistance = minSpawnDistance,
                useSlopeFilter = useSlopeFilter,
                slopeRangeRad = math.radians(slopeRange),
                useHeightFilter = useHeightFilter,
                heightRange = heightRange,
                validIndices = validIndices
            }.Run(spawnPositions.Length);

            // Spawn prefabs at valid positions
            for (int i = 0; i < validIndices.Length; i++) {
                int validIndex = validIndices[i];
                RaycastHit hit = results[validIndex];

                SpawnPrefabAtLocation(hit, prefab, parentGroup);
            }

            // Cleanup native arrays
            commands.Dispose();
            results.Dispose();
            validIndices.Dispose();
        }

        void SpawnPrefabAtLocation(RaycastHit hit, GameObject prefab, Transform fallbackParentGroup) {
            if (IsSurfacePaintable(hit) == false) {
                return;
            }

            // Check minimum distance against same prefab type only
            if (MapPainterUtility.IsPrefabTooClose(hit.point, minSpawnDistance, -1, prefab)) {
                return;
            }

            // Create instance
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, fallbackParentGroup);
            instance.transform.position = hit.point;

            MapPainterUtility.AlignToSurfaceNormal(instance.transform, hit.normal, randomRotation);

            if (randomScale) {
                float scale = UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
                instance.transform.localScale = Vector3.one * scale;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Paint Prefab");
        }

        void EraseSelectedPrefab(float3 center) {
            if (selectedPrefabIndex == -1) {
                return;
            }

            GameObject selectedPrefab = selectedPrefabs[selectedPrefabIndex];
            var selectedPrefabTransform = selectedPrefab.transform;

            Transform paintedObjectsGroup = null;

            if (useManualGroups && manualGroups.Count > 0) {
                if (selectedGroupIndex  != -1) {
                    paintedObjectsGroup = manualGroups[selectedGroupIndex].transform;
                }
            } else {
                paintedObjectsGroup = GameObject.Find(parentGroupName)?.transform;
            }

            if (paintedObjectsGroup == null) {
                return;
            }

            var brushSizeSq = math.square(brushSize);
            for (int i = paintedObjectsGroup.childCount - 1; i >= 0; i--) {
                var child = paintedObjectsGroup.GetChild(i);
                var rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                if (rootPrefab == null) {
                    continue;
                }
                if (rootPrefab.transform.root == selectedPrefabTransform) {
                    var distanceSq = math.distancesq(center, child.position);
                    if (distanceSq <= brushSizeSq) {
                        Undo.DestroyObjectImmediate(child.gameObject);
                    }
                }
            }
        }

        void EraseAllPrefabs(Vector3 center) {
            Transform paintedObjectsGroup = null;

            if (useManualGroups && manualGroups.Count > 0) {
                if (selectedGroupIndex  != -1) {
                    paintedObjectsGroup = manualGroups[selectedGroupIndex].transform;
                }
            } else {
                paintedObjectsGroup = GameObject.Find(parentGroupName)?.transform;
            }

            if (paintedObjectsGroup == null) {
                return;
            }

            var brushSizeSq = math.square(brushSize);
            for (int i = paintedObjectsGroup.childCount - 1; i >= 0; i--) {
                var child = paintedObjectsGroup.GetChild(i);
                var distanceSq = math.distancesq(center, child.position);
                if (distanceSq <= brushSizeSq) {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        void TrimPrefabs(Vector3 center) {
            if (selectedPrefabIndex == -1) {
                return;
            }

            GameObject selectedPrefab = selectedPrefabs[selectedPrefabIndex];
            var selectedPrefabTransform = selectedPrefab.transform;

            Transform paintedObjectsGroup = null;

            if (useManualGroups && manualGroups.Count > 0) {
                if (selectedGroupIndex  != -1) {
                    paintedObjectsGroup = manualGroups[selectedGroupIndex].transform;
                }
            } else {
                paintedObjectsGroup = GameObject.Find(parentGroupName)?.transform;
            }

            if (paintedObjectsGroup == null) {
                return;
            }

            var objectsInRange = new List<GameObject>(64);
            var brushSizeSq = math.square(brushSize);
            for (int i = paintedObjectsGroup.childCount - 1; i >= 0; i--) {
                var child = paintedObjectsGroup.GetChild(i);
                var rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                if (rootPrefab == null) {
                    continue;
                }
                if (rootPrefab.transform.root == selectedPrefabTransform) {
                    var distanceSq = math.distancesq(center, child.position);
                    if (distanceSq <= brushSizeSq) {
                        objectsInRange.Add(child.gameObject);
                    }
                }
            }

            if (objectsInRange.Count == 0) {
                return;
            }

            var objectsToRemove = (int)math.round(objectsInRange.Count * trimRate);

            if (objectsToRemove == 0) {
                objectsToRemove = 1;
            }

            for (int i = 0; i < objectsToRemove; i++) {
                int randomIndex = UnityEngine.Random.Range(0, objectsInRange.Count);
                Undo.DestroyObjectImmediate(objectsInRange[randomIndex]);
            }
        }

        int CalculateBaseDensity() {
            float brushArea = math.PI * math.square(brushSize);
            int densityFromSize = (int)math.round(brushArea * 0.5f);

            return math.clamp(densityFromSize, 1, maxDensity);
        }

        int CalculateSpawnCount() {
            int baseDensity = CalculateBaseDensity();
            int actualCount = (int)math.round(baseDensity * spawnRate);

            return math.max(actualCount, 1);
        }

        void CountPaintedObjects() {
            Transform paintedObjectsGroup = GameObject.Find(parentGroupName)?.transform;
            int count = 0;

            if (paintedObjectsGroup != null) {
                count = paintedObjectsGroup.childCount;
            }

            EditorUtility.DisplayDialog("Painted Objects Count",
                $"Found {count} objects in the '{parentGroupName}' group.",
                "OK");
        }

        void SelectAllPaintedObjects() {
            Transform paintedObjectsGroup = GameObject.Find(parentGroupName)?.transform;

            if (paintedObjectsGroup != null && paintedObjectsGroup.childCount > 0) {
                Object[] gameObjects = new Object[paintedObjectsGroup.childCount];
                for (int i = 0; i < paintedObjectsGroup.childCount; i++) {
                    gameObjects[i] = paintedObjectsGroup.GetChild(i).gameObject;
                }

                Selection.objects = gameObjects;
            }
        }

        void ClearAllPaintedObjects() {
            Transform paintedObjectsGroup = GameObject.Find(parentGroupName)?.transform;

            if (paintedObjectsGroup != null && paintedObjectsGroup.childCount > 0) {
                for (int i = paintedObjectsGroup.childCount - 1; i >= 0; i--) {
                    var child = paintedObjectsGroup.GetChild(i);
                    Undo.DestroyObjectImmediate(child.gameObject);
                }

                ValidateGroups();
            }
        }

        void SaveCurrentSettings() {
            if (selectedPrefabIndex != -1) {

                // Save current settings to the prefab's slot
                PrefabSettings settings = prefabSettingsList[selectedPrefabIndex];
                settings.brushSize = brushSize;
                settings.maxDensity = maxDensity;
                settings.spawnRate = spawnRate;
                settings.minSpawnDistance = minSpawnDistance;
                settings.trimRate = trimRate;
                settings.randomRotation = randomRotation;
                settings.randomScale = randomScale;
                settings.scaleRange = scaleRange;
                settings.distributionPattern = distributionPattern;
            }

            // Also sync with profile if available
            if (currentProfile != null) {
                SyncWorkingVariablesToProfile();
            }
        }

        void SyncWorkingVariablesToProfile() {
            if (currentProfile == null) return;

            currentProfile.prefabs = new List<GameObject>(selectedPrefabs);
            currentProfile.prefabSettings = new List<PrefabSettings>(prefabSettingsList);
            currentProfile.selectedPrefabIndex = selectedPrefabIndex;
            currentProfile.brushSize = brushSize;
            currentProfile.maxDensity = maxDensity;
            currentProfile.spawnRate = spawnRate;
            currentProfile.minSpawnDistance = minSpawnDistance;
            currentProfile.trimRate = trimRate;
            currentProfile.randomRotation = randomRotation;
            currentProfile.randomScale = randomScale;
            currentProfile.scaleRange = scaleRange;
            currentProfile.distributionPattern = distributionPattern;
            currentProfile.useSlopeFilter = useSlopeFilter;
            currentProfile.slopeRange = slopeRange;
            currentProfile.useHeightFilter = useHeightFilter;
            currentProfile.heightRange = heightRange;
            currentProfile.useParentGroups = useParentGroups;
            currentProfile.parentGroupName = parentGroupName;
            currentProfile.useManualGroups = useManualGroups;
            currentProfile.showAllGroups = showAllGroups;
            currentProfile.manualGroups = new List<GameObject>(manualGroups);
            currentProfile.selectedGroupIndex = selectedGroupIndex;
            currentProfile.showAdvancedSettings = _showAdvancedSettings;
            currentProfile.canPaintByDrag = canPaintByDrag;
        }

        void LoadSettingsForCurrentPrefab() {
            if (selectedPrefabIndex != -1) {
                PrefabSettings settings = prefabSettingsList[selectedPrefabIndex];
                brushSize = settings.brushSize;
                maxDensity = settings.maxDensity;
                spawnRate = settings.spawnRate;
                minSpawnDistance = settings.minSpawnDistance;
                trimRate = settings.trimRate;
                randomRotation = settings.randomRotation;
                randomScale = settings.randomScale;
                scaleRange = settings.scaleRange;
                distributionPattern = settings.distributionPattern;

                Repaint();
            }
        }

        void RemovePrefabAt(int index) {
            selectedPrefabs.RemoveAt(index);
            prefabSettingsList.RemoveAt(index);

            // Adjust selected index
            if (index < selectedPrefabIndex) {
                --selectedPrefabIndex;
            } else if (selectedPrefabIndex == index) {
                selectedPrefabIndex = math.min(selectedPrefabIndex, selectedPrefabs.Count - 1);
            }

            // Load settings for the new current prefab
            if (selectedPrefabIndex != -1) {
                LoadSettingsForCurrentPrefab();
            }
        }

        bool IsSurfacePaintable(RaycastHit hit) {
            var hitObject = hit.collider.gameObject;
            var layer = hitObject.layer;

            // Terrain layer: Always paintable
            if (layer == RenderLayers.Terrain) {
                return true;
            }

            // Walkable layer: Only paintable if object has MedusaRendererPrefab component
            if (layer == RenderLayers.Walkable) {
                MedusaRendererPrefab medusaComponent = hitObject.GetComponent<MedusaRendererPrefab>();
                return medusaComponent != null;
            }

            // All other layers: Not paintable
            return false;
        }

        void RefreshThumbnails() {
            // AssetPreviewCache.ClearCache();
        }

        void AddNewGroup() {
            var newName = newGroupName.Trim();
            if (string.IsNullOrEmpty(newName)) {
                EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid group name.", "OK");
                return;
            }

            // Create root parent if it doesn't exist
            if (_rootGroupParent == null) {
                _rootGroupParent = MapPainterUtility.GetOrCreatePaintGroup(parentGroupName);
            }

            if (_rootGroupParent.Find(newName)) {
                EditorUtility.DisplayDialog("Invalid Name", $" Group named {newName} already exists!\nPlease enter a valid group name.", "OK");
                return;
            }

            GameObject newGroup = new GameObject(newGroupName.Trim());
            newGroup.transform.position = Vector3.zero;
            newGroup.transform.SetParent(_rootGroupParent);
            Undo.RegisterCreatedObjectUndo(newGroup, "Create Manual Group");

            manualGroups.Add(newGroup);
            selectedGroupIndex = manualGroups.Count - 1;

            UpdateAllGroupsVisibility();

            newGroupName = "New Group";
        }

        void SelectGroup(int index) {
            selectedGroupIndex = index;
            UpdateAllGroupsVisibility();
        }

        void DeleteGroup(int index) {
            var groupToDelete = manualGroups[index];

            var shouldDelete = true;
            if (groupToDelete && groupToDelete.transform.childCount > 0) {
                shouldDelete = EditorUtility.DisplayDialog("Delete Group",
                    $"Are you sure you want to delete group '{groupToDelete.name}' and all its painted objects?",
                    "Yes, Delete", "Cancel");
            }

            if (!shouldDelete) {
                return;
            }

            if (groupToDelete) {
                Undo.DestroyObjectImmediate(groupToDelete);
            }

            manualGroups.RemoveAt(index);

            if (index < selectedGroupIndex) {
                --selectedGroupIndex;
            } else if (selectedGroupIndex == index) {
                selectedGroupIndex = math.min(selectedGroupIndex, manualGroups.Count - 1);
            }
        }

        Transform GetCurrentSelectedGroup() {
            if (!useManualGroups) {
                if (_rootGroupParent == null) {
                    _rootGroupParent = MapPainterUtility.GetOrCreatePaintGroup(parentGroupName);
                }
                return useParentGroups ? _rootGroupParent : null;
            }

            if (manualGroups.Count == 0) {
                newGroupName = "AutoDefaultGroup";
                AddNewGroup();
            }

            // Return the selected group
            if (selectedGroupIndex != -1) {
                return manualGroups[selectedGroupIndex].transform;
            }

            return null;
        }

        void UpdateAllGroupsVisibility() {
            if (!useManualGroups) return;

            for (int i = 0; i < manualGroups.Count; i++) {
                if (manualGroups[i] != null) {
                    bool shouldBeVisible = showAllGroups || i == selectedGroupIndex;
                    manualGroups[i].SetActive(shouldBeVisible);
                }
            }
        }

        void CreateNewProfile() {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Map Painter Profile",
                "NewMapPainterProfile",
                "asset",
                "Choose where to save the new Map Painter Profile",
                "Assets/MapPainterProfiles");

            if (!string.IsNullOrEmpty(path)) {
                var newProfile = CreateInstance<MapPainterProfile>();
                SaveCurrentSettingsToProfile(newProfile);

                AssetDatabase.CreateAsset(newProfile, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                currentProfile = newProfile;
            }
        }

        void SaveToProfile() {
            if (currentProfile == null) {
                CreateNewProfile();
                return;
            }

            SaveCurrentSettingsToProfile(currentProfile);
            EditorUtility.SetDirty(currentProfile);
            AssetDatabase.SaveAssetIfDirty(currentProfile);
        }

        void SaveCurrentSettingsToProfile(MapPainterProfile profile) {
            // Save current window state to profile
            profile.prefabs = new List<GameObject>(selectedPrefabs);
            profile.prefabSettings = new List<PrefabSettings>(prefabSettingsList);
            profile.selectedPrefabIndex = selectedPrefabIndex;

            profile.brushSize = brushSize;
            profile.maxDensity = maxDensity;
            profile.spawnRate = spawnRate;
            profile.minSpawnDistance = minSpawnDistance;
            profile.trimRate = trimRate;
            profile.randomRotation = randomRotation;
            profile.randomScale = randomScale;
            profile.scaleRange = scaleRange;
            profile.distributionPattern = distributionPattern;

            profile.useSlopeFilter = useSlopeFilter;
            profile.slopeRange = slopeRange;
            profile.useHeightFilter = useHeightFilter;
            profile.heightRange = heightRange;

            profile.useParentGroups = useParentGroups;
            profile.parentGroupName = parentGroupName;
            profile.useManualGroups = useManualGroups;
            profile.showAllGroups = showAllGroups;
            profile.manualGroups = new List<GameObject>(manualGroups);
            profile.selectedGroupIndex = selectedGroupIndex;

            profile.showAdvancedSettings = _showAdvancedSettings;
            profile.canPaintByDrag = canPaintByDrag;

            profile.ValidateSettings();
        }

        void LoadFromProfile() {
            if (currentProfile == null) return;

            currentProfile.ValidateSettings();

            // Load from profile to current window state
            selectedPrefabs = new List<GameObject>(currentProfile.prefabs);
            prefabSettingsList = new List<PrefabSettings>(currentProfile.prefabSettings);
            selectedPrefabIndex = currentProfile.selectedPrefabIndex;

            brushSize = currentProfile.brushSize;
            maxDensity = currentProfile.maxDensity;
            spawnRate = currentProfile.spawnRate;
            minSpawnDistance = currentProfile.minSpawnDistance;
            trimRate = currentProfile.trimRate;
            randomRotation = currentProfile.randomRotation;
            randomScale = currentProfile.randomScale;
            scaleRange = currentProfile.scaleRange;
            distributionPattern = currentProfile.distributionPattern;

            useSlopeFilter = currentProfile.useSlopeFilter;
            slopeRange = currentProfile.slopeRange;
            useHeightFilter = currentProfile.useHeightFilter;
            heightRange = currentProfile.heightRange;

            useParentGroups = currentProfile.useParentGroups;
            parentGroupName = currentProfile.parentGroupName;
            useManualGroups = currentProfile.useManualGroups;
            showAllGroups = currentProfile.showAllGroups;
            manualGroups = new List<GameObject>(currentProfile.manualGroups);
            selectedGroupIndex = currentProfile.selectedGroupIndex;

            _showAdvancedSettings = currentProfile.showAdvancedSettings;
            canPaintByDrag = currentProfile.canPaintByDrag;

            LoadSettingsForCurrentPrefab();
            UpdateAllGroupsVisibility();
        }

        void LoadLastProfile() {
            string lastProfilePath = EditorPrefs.GetString("MapPainter_LastProfile", "");
            if (!string.IsNullOrEmpty(lastProfilePath)) {
                currentProfile = AssetDatabase.LoadAssetAtPath<MapPainterProfile>(lastProfilePath);
                if (currentProfile != null) {
                    LoadFromProfile();
                }
            }
        }

        void SaveProfilePreference() {
            if (currentProfile != null) {
                string path = AssetDatabase.GetAssetPath(currentProfile);
                EditorPrefs.SetString("MapPainter_LastProfile", path);
                SaveToProfile(); // Auto-save current state to profile
            }
        }

        void ResetConfiguration() {
            if (currentProfile != null) {
                currentProfile.ResetToDefaults();
                EditorUtility.SetDirty(currentProfile);
                AssetDatabase.SaveAssetIfDirty(currentProfile);
                LoadFromProfile();
            } else {
                // Reset window state to defaults if no profile
                selectedPrefabs.Clear();
                selectedPrefabIndex = -1;
                prefabSettingsList.Clear();
                brushSize = 5.0f;
                maxDensity = 20;
                spawnRate = 0.3f;
                minSpawnDistance = 0.5f;
                trimRate = 0.3f;
                randomRotation = true;
                randomScale = false;
                scaleRange = new Vector2(0.8f, 1.2f);
                distributionPattern = MapPainterUtility.DistributionPattern.Random;
                useSlopeFilter = false;
                slopeRange = new Vector2(0f, 30f);
                useHeightFilter = false;
                heightRange = new Vector2(0f, 100f);
                useParentGroups = true;
                parentGroupName = "Painted Objects";
                useManualGroups = true;
                showAllGroups = false;
                manualGroups.Clear();
                selectedGroupIndex = -1;
                _showAdvancedSettings = false;
                _paintModeEnabled = false;
            }

            Repaint();
        }

        void OnEditorApplicationOnplayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.EnteredEditMode) {
                SceneView.duringSceneGui += OnSceneGUI;
            } else {
                SceneView.duringSceneGui -= OnSceneGUI;
            }
        }
    }

    [Serializable]
    public class PrefabSettings {
        public float brushSize = 5f;
        public int maxDensity = 20;
        public float spawnRate = 0.3f;
        public float minSpawnDistance = 0.5f;
        public float trimRate = 0.3f;
        public bool randomRotation = true;
        public bool randomScale = false;
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        public MapPainterUtility.DistributionPattern distributionPattern = MapPainterUtility.DistributionPattern.Random;
    }
}

