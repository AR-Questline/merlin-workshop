using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.Previews;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Awaken.Utility.UI;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Prefabs {
    public class UniqueMeshesFinderWindow : EditorWindow {
        const string WhitelistMeshesFile = "UniqueMeshesWhitelist.json";
        const int TableHeight = 512;

        static ARGameObjectPreview s_preview = new ARGameObjectPreview();
        static Object[] s_targets = new Object[1];

        OnDemandCache<Scene, string> _sceneNameCache = new OnDemandCache<Scene, string>(static scene => scene.name.Replace("_", " "));

        GUIStyle _buttonStyle;
        GUIStyle _warningLabelStyle;

        ImguiTable<GameObject> _table = new ImguiTable<GameObject>();
        Vector2 _tableScroll;

        Vector2 _wholeScroll;

        List<GameObject> _uniques = new List<GameObject>();
        List<GameObject> _filtered = new List<GameObject>();
        int _minimumUsages = 2;

        GameObject _selected;
        int _selectedIndex = -1;

        GameObject _replacementObject;

        HashSet<Mesh> _whitelistedMeshes = new HashSet<Mesh>();

        WhitelistFilter _whitelistFilter = WhitelistFilter.All;
        XboxReplaceFilter _xboxReplaceFilter = XboxReplaceFilter.All;
        XboxDeleteFilter _xboxDeleteFilter = XboxDeleteFilter.All;

        void OnEnable() {
            Initialize();
        }

        void OnDisable() {
            SceneView.duringSceneGui -= OnSceneGUI;
            _table.Dispose();
        }

        void Initialize() {
            LoadWhitelist();

            _table = new ImguiTable<GameObject>(SearchPrediction,
                128,
                ImguiTable<GameObject>.ColumnDefinition.Create("Preview", 128, DrawPreview, Name),
                ImguiTable<GameObject>.ColumnDefinition.Create("Object", 176, DrawObject, Name),
                ImguiTable<GameObject>.ColumnDefinition.Create("Scene", 128, DrawScene, static o => o.scene.name),
                ImguiTable<GameObject>.ColumnDefinition.CreateNumeric("Distance", 78, ImguiTableUtils.FloatTwoDrawer, DistanceToObject),
                ImguiTable<GameObject>.ColumnDefinition.CreateNumeric("Volume", 64, ImguiTableUtils.FloatTwoDrawer, ObjectVolume),
                ImguiTable<GameObject>.ColumnDefinition.CreateNumeric("Max axis", 64, ImguiTableUtils.FloatTwoDrawer, ObjectMaxAxis),
                ImguiTable<GameObject>.ColumnDefinition.Create("Select", 64, DrawSelect, Name),
                ImguiTable<GameObject>.ColumnDefinition.Create("Whitelist", 48, DrawWhitelist, Name), //7
                ImguiTable<GameObject>.ColumnDefinition.Create("X-replace", 54, DrawXboxReplacement, Name), //8
                ImguiTable<GameObject>.ColumnDefinition.Create("X-delete", 54, DrawXboxDelete, Name) //9
                ) {
                    ShowFooter = false,
                };
            _table.OnSearchChanged += OnFilterChanged;
            _table.SetSort(4, true);

            static string Name(GameObject meshObject) => meshObject.name;

            _selectedIndex = -1;
            _selected = null;

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnGUI() {
            _buttonStyle ??= new GUIStyle(GUI.skin.button) {
                richText = true
            };

            _warningLabelStyle ??= new GUIStyle(GUI.skin.label) {
                richText = true,
                wordWrap = true
            };

            _wholeScroll = EditorGUILayout.BeginScrollView(_wholeScroll);

            DrawFilters();
            DrawTable();

            if (_selected) {
                DrawSelected();

                // Divider
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }

            DrawControl();

            EditorGUILayout.EndScrollView();

            ProcessInput();
        }

        void DrawFilters() {
            var columns = _table.VisibleColumns;

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _whitelistFilter = (WhitelistFilter)EditorGUILayout.EnumPopup("Whitelist Filter", _whitelistFilter);
            if (EditorGUI.EndChangeCheck()) {
                OnFilterChanged();
            }

            EditorGUI.BeginChangeCheck();
            _xboxReplaceFilter = (XboxReplaceFilter)EditorGUILayout.EnumPopup("Xbox Replace Filter", _xboxReplaceFilter);
            if (EditorGUI.EndChangeCheck()) {
                OnFilterChanged();
            }
            columns[8] = _xboxReplaceFilter == XboxReplaceFilter.All;

            EditorGUI.BeginChangeCheck();
            _xboxDeleteFilter = (XboxDeleteFilter)EditorGUILayout.EnumPopup("Xbox Delete Filter", _xboxDeleteFilter);
            if (EditorGUI.EndChangeCheck()) {
                OnFilterChanged();
            }
            columns[9] = _xboxDeleteFilter == XboxDeleteFilter.All;
            GUILayout.EndHorizontal();
        }

        void DrawTable() {
            _tableScroll = EditorGUILayout.BeginScrollView(_tableScroll, GUILayout.Height(TableHeight));

            if (_table.Draw(_uniques, TableHeight, _tableScroll.y, position.width)) {
                _uniques.Sort(_table.Sorter);
                OnFilterChanged();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField($"{_selectedIndex+1}/{_filtered.Count}", GUILayout.Width(64));
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.EndHorizontal();
        }

        void DrawSelected() {
            var fullRect = (PropertyDrawerRects)EditorGUILayout.GetControlRect(GUILayout.Height(256));
            var isWhitelisted = IsWhitelisted(_selected);
            var selectedColor = isWhitelisted ? new Color(0.3f, 0.3f, 0.3f, 0.9f) : new Color(0.25f, 0.1f, 0.1f, 0.9f);
            EditorGUI.DrawRect(fullRect.Rect, selectedColor);
            // Title
            EditorGUI.LabelField(fullRect.AllocateTop(EditorGUIUtility.singleLineHeight), "Selected Object");
            // Header
            var headerRect = (PropertyDrawerRects)fullRect.AllocateTop(EditorGUIUtility.singleLineHeight);

            var toLeft = headerRect.AllocateLeft(64);
            var toRight = headerRect.AllocateRight(64);
            if (GUI.Button(toLeft, "\u25C0 Prev", _buttonStyle)) {
                SelectLeft();
            }
            if (GUI.Button(toRight, "Next \u25B6", _buttonStyle)) {
                SelectRight();
            }

            EditorGUI.BeginChangeCheck();
            _selectedIndex = EditorGUI.IntSlider(headerRect.Rect, _selectedIndex, 0, _filtered.Count-1);
            if (EditorGUI.EndChangeCheck()) {
                Select(_selectedIndex);
            }
            // Preview
            var leftRect = fullRect.AllocateLeft(position.width / 2 - 8);
            DrawPreview(leftRect, _selected);
            // Info
            var rightRect = (PropertyDrawerRects)fullRect.AllocateRight(position.width / 2 - 8);
            Divider(ref rightRect);

            var infoRect = (PropertyDrawerRects)rightRect.AllocateTop(EditorGUIUtility.singleLineHeight);
            var whiteListRect = infoRect.AllocateRight(72);
            DrawObject(infoRect.Rect, _selected);

            EditorGUI.BeginChangeCheck();
            EditorGUI.ToggleLeft(whiteListRect, "Whitelist", isWhitelisted);
            if (EditorGUI.EndChangeCheck()) {
                SetWhitelisted(_selected, !isWhitelisted);
            }

            infoRect = rightRect.AllocateTop(EditorGUIUtility.singleLineHeight);
            var xboxReplaceRect = infoRect.AllocateLeftNormalized(0.49f);
            var xboxDeleteRect = infoRect.AllocateRightNormalized(0.49f);

            GUI.enabled = false;
            EditorGUI.ObjectField(xboxReplaceRect, "X-replace", _selected.GetComponent<XboxReplace>()?.replacement, typeof(GameObject), false);
            EditorGUI.ToggleLeft(xboxDeleteRect, "X-delete", _selected.GetComponent<XboxDelete>());
            GUI.enabled = !isWhitelisted;

            // Delete
            Divider(ref rightRect);

            var deleteButtonsOriginalRect = (PropertyDrawerRects)rightRect.AllocateTop(EditorGUIUtility.singleLineHeight);
            var deleteButtonsCopyRect = deleteButtonsOriginalRect;
            var permanentDeleteButtonRect = deleteButtonsCopyRect.AllocateLeftNormalized(0.45f);
            var xboxDeleteButtonRect = deleteButtonsOriginalRect.AllocateRightNormalized(0.45f);

            if (GUI.Button(permanentDeleteButtonRect, "Permanent delete")) {
                PermanentDelete();
            }

            if (GUI.Button(xboxDeleteButtonRect, "Xbox delete")) {
                XboxDelete();
            }

            GUI.enabled = !isWhitelisted;

            // Replacement
            Divider(ref rightRect);

            var replaceObjectRect = rightRect.AllocateTop(EditorGUIUtility.singleLineHeight);

            _replacementObject = (GameObject)EditorGUI.ObjectField(replaceObjectRect, "Replacement Object", _replacementObject, typeof(GameObject), false);

            GUI.enabled = _replacementObject && !isWhitelisted;

            var replaceButtonsOriginalRect = (PropertyDrawerRects)rightRect.AllocateTop(EditorGUIUtility.singleLineHeight);
            var replaceButtonsCopyRect = replaceButtonsOriginalRect;
            var permanentReplaceButtonRect = replaceButtonsCopyRect.AllocateLeftNormalized(0.45f);
            var xboxReplaceButtonRect = replaceButtonsOriginalRect.AllocateRightNormalized(0.45f);

            if (GUI.Button(permanentReplaceButtonRect, "Permanent replace")) {
                PermanentReplace();
            }

            if (GUI.Button(xboxReplaceButtonRect, "Xbox replace")) {
                XboxReplace();
            }

            GUI.enabled = true;

            void Divider(ref PropertyDrawerRects rect) {
                EditorGUI.LabelField(rect.AllocateTop(EditorGUIUtility.singleLineHeight), "", GUI.skin.horizontalSlider);
            }
        }

        void DrawControl() {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _minimumUsages = EditorGUILayout.IntSlider("Minimum Usages", _minimumUsages, 1, 5);
            if (EditorGUI.EndChangeCheck()) {
                CollectUniques();
            }
            if (GUILayout.Button("Collect Uniques")) {
                CollectUniques();
            }
            GUILayout.EndHorizontal();
        }

        // === Operation
        void CollectUniques() {
            var lods = new List<DrakeLodGroup>(512);
            var singularMeshes = new List<DrakeMeshRenderer>(512);
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) {
                    continue;
                }
                GameObjects.FindComponentsByTypeInScene(scene, false, ref lods);
                GameObjects.FindComponentsByTypeInScene(scene, false, ref singularMeshes);
            }

            var meshesUsage = new Dictionary<Mesh, int>();
            foreach (var drakeLodGroup in lods) {
                foreach (var drakeMeshRenderer in drakeLodGroup.Renderers) {
                    if ((drakeMeshRenderer.LodMask & 1) == 0) {
                        continue;
                    }
                    IncrementMesh(meshesUsage, drakeMeshRenderer);
                }
            }
            foreach (var singularMesh in singularMeshes) {
                if (singularMesh.Parent) {
                    continue;
                }
                IncrementMesh(meshesUsage, singularMesh);
            }

            _uniques.Clear();

            foreach (var drakeLodGroup in lods) {
                if (!IsPossiblyStatic(drakeLodGroup.gameObject)) {
                    continue;
                }

                bool isUnique = true;
                foreach (var drakeMeshRenderer in drakeLodGroup.Renderers) {
                    if ((drakeMeshRenderer.LodMask & 1) == 0) {
                        continue;
                    }
                    var mesh = drakeMeshRenderer.EDITOR_GetMesh();
                    var usages = meshesUsage.GetValueOrDefault(mesh, 0);
                    if (usages > _minimumUsages) {
                        isUnique = false;
                        break;
                    }
                }
                if (isUnique) {
                    _uniques.Add(drakeLodGroup.gameObject);
                }
            }
            foreach (var singularMesh in singularMeshes) {
                if (singularMesh.Parent) {
                    continue;
                }

                if (!IsPossiblyStatic(singularMesh.gameObject)) {
                    continue;
                }

                var mesh = singularMesh.EDITOR_GetMesh();
                var usages = meshesUsage.GetValueOrDefault(mesh, 0);
                if (usages <= _minimumUsages) {
                    _uniques.Add(singularMesh.gameObject);
                }
            }

            _uniques.Sort(_table.Sorter);
            OnFilterChanged();

            static void IncrementMesh(Dictionary<Mesh, int> meshesUsage, DrakeMeshRenderer drake) {
                var mesh = drake.EDITOR_GetMesh();
                if (mesh == null) {
                    return;
                }

                if (!meshesUsage.TryAdd(mesh, 1)) {
                    meshesUsage[mesh]++;
                }
            }

            bool IsPossiblyStatic(GameObject go) {
                return WillBeStatic(go);
            }

            bool WillBeStatic(GameObject go) {
                if (ScenesStaticSubdivision.IsNonStaticObject(go)) {
                    return false;
                }
                var parent = go.transform.parent;
                if (parent == null) {
                    return true;
                }
                return WillBeStatic(parent.gameObject);
            }
        }

        void OnFilterChanged() {
            _filtered.Clear();
            foreach (var unique in _uniques) {
                if (unique && SearchPrediction(unique, _table.SearchContext)) {
                    _filtered.Add(unique);
                }
            }

            if (_selected) {
                var index = _filtered.IndexOf(_selected);
                if (index >= 0) {
                    _selectedIndex = index;
                } else {
                    Select(0);
                }
            }
        }

        void Select(int index) {
            if (index >= 0 && index < _filtered.Count) {
                _selectedIndex = index;
                _selected = _filtered[index];

                Selection.activeObject = _selected;
                EditorGUIUtility.PingObject(_selected);
                SceneView.lastActiveSceneView.FrameSelected(true, true);

                var notification = $"Selected {index + 1}/{_filtered.Count} {_selected.name}";
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent(notification));

                _tableScroll.y = _table.Frame(_filtered, _selectedIndex, TableHeight, false);
            } else {
                _selectedIndex = -1;
                _selected = null;
            }
        }

        void SelectLeft() {
            Select((_selectedIndex - 1 + _filtered.Count) % _filtered.Count);
        }

        void SelectRight() {
            Select((_selectedIndex + 1) % _filtered.Count);
        }

        void PermanentReplace() {
            if (!_selected) {
                return;
            }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Permanent replace unique mesh");

            var newObject = Instantiate(_replacementObject, _selected.transform.position, _selected.transform.rotation, _selected.transform.parent);
            Undo.RegisterCreatedObjectUndo(_replacementObject, "Create replacement object");
            newObject.transform.localScale = _selected.transform.localScale;
            Undo.RegisterFullObjectHierarchyUndo(newObject, "Update my GameObject position");
            Undo.DestroyObjectImmediate(_selected);

            Undo.SetCurrentGroupName("Replace unique mesh");

            SelectRight();
            OnFilterChanged();

            Undo.CollapseUndoOperations(group);
        }

        void XboxReplace() {
            if (!_selected) {
                return;
            }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Xbox replace unique mesh");

            var xboxReplace = _selected.GetComponent<XboxReplace>();
            if (!xboxReplace) {
                xboxReplace = Undo.AddComponent<XboxReplace>(_selected);
            }
            Undo.RecordObject(xboxReplace, "Change XboxReplace");
            xboxReplace.replacement = _replacementObject;
            SelectRight();
            if (_xboxReplaceFilter != XboxReplaceFilter.All) {
                OnFilterChanged();
            }

            Undo.CollapseUndoOperations(group);
        }

        void PermanentDelete() {
            if (!_selected) {
                return;
            }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Permanent delete unique mesh");

            Undo.DestroyObjectImmediate(_selected);
            SelectRight();
            OnFilterChanged();

            Undo.CollapseUndoOperations(group);
        }

        void XboxDelete() {
            if (!_selected || _selected.GetComponent<XboxDelete>()) {
                return;
            }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Xbox delete unique mesh");

            Undo.AddComponent<XboxDelete>(_selected);
            SelectRight();
            if (_xboxDeleteFilter != XboxDeleteFilter.All) {
                OnFilterChanged();
            }

            Undo.CollapseUndoOperations(group);
        }

        bool SearchPrediction(GameObject o, SearchPattern searchContext) {
            if (!o) {
                return false;
            }
            if (!PassFilters(o)) {
                return false;
            }

            if (searchContext.IsEmpty) {
                return true;
            }

            foreach (var search in searchContext.SearchParts) {
                if (StartWith(search, "scene:")) {
                    if (o.scene.name.Contains(search.Substring(6), StringComparison.InvariantCultureIgnoreCase)) {
                        return true;
                    }
                } else if (StartWith(search, "name:")) {
                    if (o.name.Contains(search.Substring(5), StringComparison.InvariantCultureIgnoreCase)) {
                        return true;
                    }
                } else {
                    if (o.name.Contains(search, StringComparison.InvariantCultureIgnoreCase) || o.scene.name.Contains(search, StringComparison.InvariantCultureIgnoreCase)) {
                        return true;
                    }
                }
            }

            return false;

            bool PassFilters(GameObject go) {
                if (!WhitelistFilter(go)) {
                    return false;
                }

                if (!XboxReplaceFilter(go)) {
                    return false;
                }

                if (!XboxDeleteFilter(go)) {
                    return false;
                }

                return true;
            }

            bool WhitelistFilter(GameObject go) {
                if (_whitelistFilter == UniqueMeshesFinderWindow.WhitelistFilter.All) {
                    return true;
                }
                if (_whitelistFilter == UniqueMeshesFinderWindow.WhitelistFilter.Whitelisted) {
                    return IsWhitelisted(go);
                }
                if (_whitelistFilter == UniqueMeshesFinderWindow.WhitelistFilter.NotWhitelisted) {
                    return !IsWhitelisted(go);
                }

                return false;
            }

            bool XboxReplaceFilter(GameObject go) {
                if (_xboxReplaceFilter == UniqueMeshesFinderWindow.XboxReplaceFilter.All) {
                    return true;
                }
                if (_xboxReplaceFilter == UniqueMeshesFinderWindow.XboxReplaceFilter.XboxReplace) {
                    return go.GetComponent<XboxReplace>();
                }
                if (_xboxReplaceFilter == UniqueMeshesFinderWindow.XboxReplaceFilter.NotXboxReplace) {
                    return !go.GetComponent<XboxReplace>();
                }

                return false;
            }

            bool XboxDeleteFilter(GameObject go) {
                if (_xboxDeleteFilter == UniqueMeshesFinderWindow.XboxDeleteFilter.All) {
                    return true;
                }
                if (_xboxDeleteFilter == UniqueMeshesFinderWindow.XboxDeleteFilter.XboxDelete) {
                    return go.GetComponent<XboxDelete>();
                }
                if (_xboxDeleteFilter == UniqueMeshesFinderWindow.XboxDeleteFilter.NotXboxDelete) {
                    return !go.GetComponent<XboxDelete>();
                }

                return false;
            }

            bool StartWith(string content, string pattern) {
                if (pattern.Length > content.Length) {
                    return false;
                }
                for (var i = 0; i < pattern.Length; i++) {
                    if (char.ToLower(pattern[i]) != char.ToLower(content[i])) {
                        return false;
                    }
                }
                return true;
            }
        }

        // === Drawers
        void DrawPreview(in Rect rect, GameObject meshObject) {
            if (meshObject == null) {
                return;
            }
            s_targets[0] = meshObject;
            s_preview.Initialize(s_targets);

            s_preview.OnPreviewGUI(rect, GUIStyle.none);

            s_preview.Cleanup();
        }

        void DrawObject(in Rect rect, GameObject meshObject) {
            if (meshObject == null) {
                return;
            }
            GUI.enabled = false;
            EditorGUI.ObjectField(rect, meshObject, typeof(GameObject), false);
            GUI.enabled = true;
        }

        void DrawScene(in Rect rect, GameObject meshObject) {
            if (meshObject == null) {
                return;
            }
            GUI.Label(rect, _sceneNameCache[meshObject.scene], _warningLabelStyle);
        }

        void DrawSelect(in Rect rect, GameObject meshObject) {
            if (meshObject == null) {
                return;
            }

            var oldColor = GUI.color;
            if (_selected == meshObject) {
                GUI.enabled = false;
                GUI.color = Color.gray;
            }

            if (GUI.Button(rect, "Select")) {
                Select(_filtered.IndexOf(meshObject));
            }

            GUI.enabled = true;
            GUI.color = oldColor;
        }

        void DrawWhitelist(in Rect rect, GameObject meshObject) {
            if (meshObject == null) {
                return;
            }

            var centerRect = rect;
            centerRect.x += (rect.width - 16) / 2;
            centerRect.width = 16;

            EditorGUI.BeginChangeCheck();
            var newWhitelisted = EditorGUI.Toggle(centerRect, IsWhitelisted(meshObject));
            if (EditorGUI.EndChangeCheck()) {
                SetWhitelisted(meshObject, newWhitelisted);
            }
        }

        void DrawXboxReplacement(in Rect rect, GameObject meshObject) {
            if (meshObject == null) {
                return;
            }

            var centerRect = rect;
            centerRect.x += (rect.width - 16) / 2;
            centerRect.width = 16;

            GUI.enabled = false;
            EditorGUI.Toggle(centerRect, meshObject.GetComponent<XboxReplace>());
            GUI.enabled = true;
        }

        void DrawXboxDelete(in Rect rect, GameObject meshObject) {
            if (meshObject == null) {
                return;
            }

            var centerRect = rect;
            centerRect.x += (rect.width - 16) / 2;
            centerRect.width = 16;

            GUI.enabled = false;
            EditorGUI.Toggle(centerRect, meshObject.GetComponent<XboxDelete>());
            GUI.enabled = true;
        }

        // === Values
        float DistanceToObject(GameObject meshObject) {
            if (meshObject == null) {
                return -1;
            }
            var mainCamera = SceneView.lastActiveSceneView.camera;
            if (!mainCamera) {
                return 0;
            }
            return math.distance(mainCamera.transform.position, meshObject.transform.position);
        }

        float ObjectVolume(GameObject meshObject) {
            if (meshObject == null) {
                return -1;
            }
            var meshRenderer = meshObject.GetComponent<DrakeMeshRenderer>();
            return meshRenderer.WorldBounds.Volume();
        }

        float ObjectMaxAxis(GameObject meshObject) {
            if (meshObject == null) {
                return -1;
            }
            var meshRenderer = meshObject.GetComponent<DrakeMeshRenderer>();
            return math.cmax(meshRenderer.WorldBounds.Size());
        }

        // === Scene GUI
        void OnSceneGUI(SceneView sceneView) {
            ProcessInput();

            Repaint();
        }

        void ProcessInput() {
            Event e = Event.current;
            if (e.type == EventType.KeyDown) {
                if (e.keyCode == KeyCode.LeftArrow) {
                    SelectLeft();
                    e.Use();
                }
                if (e.keyCode == KeyCode.RightArrow) {
                    SelectRight();
                    e.Use();
                }
                if (e.keyCode == KeyCode.Backspace) {
                    XboxDelete();
                }
                if (e.keyCode == KeyCode.Delete) {
                    PermanentDelete();
                }
            }
        }

        // === Whitelist
        bool IsWhitelisted(GameObject meshObject) {
            var lodGroup = meshObject.GetComponent<DrakeLodGroup>();
            if (lodGroup) {
                var allWhitelisted = true;
                for (int i = 0; allWhitelisted && i < lodGroup.Renderers.Length; i++) {
                    DrakeMeshRenderer renderer = lodGroup.Renderers[i];
                    if ((renderer.LodMask & 1) == 0) {
                        continue;
                    }
                    allWhitelisted = _whitelistedMeshes.Contains(renderer.EDITOR_GetMesh());
                }
                return allWhitelisted;
            }

            var meshRenderer = meshObject.GetComponent<DrakeMeshRenderer>();
            if (meshRenderer) {
                return _whitelistedMeshes.Contains(meshRenderer.EDITOR_GetMesh());
            }

            return false;
        }

        void SetWhitelisted(GameObject meshObject, bool shouldWhitelist) {
            var lodGroup = meshObject.GetComponent<DrakeLodGroup>();
            var meshRenderer = meshObject.GetComponent<DrakeMeshRenderer>();

            if (lodGroup) {
                for (int i = 0; i < lodGroup.Renderers.Length; i++) {
                    DrakeMeshRenderer renderer = lodGroup.Renderers[i];
                    if ((renderer.LodMask & 1) == 0) {
                        continue;
                    }
                    var mesh = renderer.EDITOR_GetMesh();
                    if (shouldWhitelist) {
                        _whitelistedMeshes.Add(mesh);
                    } else {
                        _whitelistedMeshes.Remove(mesh);
                    }
                }
            } else if (meshRenderer) {
                var mesh = meshRenderer.EDITOR_GetMesh();
                if (shouldWhitelist) {
                    _whitelistedMeshes.Add(mesh);
                } else {
                    _whitelistedMeshes.Remove(mesh);
                }
            }

            var notification = shouldWhitelist ? $"Whitelisted {meshObject.name}" : $"Removed from whitelist {meshObject.name}";
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(notification));

            SaveWhitelist();

            if (_whitelistFilter != WhitelistFilter.All) {
                OnFilterChanged();
            }
        }

        void SaveWhitelist() {
            var directory = Path.Combine(Application.dataPath, "../", "Library", "__Tinder", SceneManager.GetActiveScene().name);
            var path = Path.Combine(directory, WhitelistMeshesFile);
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var meshes = new List<Whitelist>();
            foreach (var mesh in _whitelistedMeshes) {
                var meshPath = AssetDatabase.GetAssetPath(mesh);
                var meshName = mesh.name;
                meshes.Add(new Whitelist {
                    meshPath = meshPath,
                    meshName = meshName,
                });
            }

            var json = JsonConvert.SerializeObject(meshes);
            File.WriteAllText(path, json);
        }

        void LoadWhitelist() {
            var path = Path.Combine(Application.dataPath, "../", "Library", "__Tinder", SceneManager.GetActiveScene().name, WhitelistMeshesFile);
            if (!File.Exists(path)) {
                return;
            }

            var json = File.ReadAllText(path);
            try {
                var meshes = JsonConvert.DeserializeObject<List<Whitelist>>(json);

                _whitelistedMeshes.Clear();
                foreach (var mesh in meshes) {
                    var asset = AssetDatabase.LoadAllAssetsAtPath(mesh.meshPath).OfType<Mesh>().FirstOrDefault(m => m.name == mesh.meshName);
                    if (asset) {
                        _whitelistedMeshes.Add(asset);
                    }
                }
            } catch {
                // ignored
            }
        }

        [Serializable]
        struct Whitelist {
            public string meshPath;
            public string meshName;
        }

        enum WhitelistFilter : byte {
            All = 0,
            Whitelisted = 1,
            NotWhitelisted = 2,
        }

        enum XboxReplaceFilter : byte {
            All = 0,
            XboxReplace = 1,
            NotXboxReplace = 2,
        }

        enum XboxDeleteFilter : byte {
            All = 0,
            XboxDelete = 1,
            NotXboxDelete = 2,
        }

        // === Scaffolding
        [MenuItem("TG/Assets/Unique Meshes Finder")]
        static void ShowWindow() {
            var window = GetWindow<UniqueMeshesFinderWindow>();
            window.titleContent = new GUIContent("Unique Meshes Finder");
            window.Show();
        }

        [InitializeOnLoadMethod] // Runs after compilation
        static void OnScriptReload() {
            // Get all open EditorWindows of this type and reinitialize them
            var windows = Resources.FindObjectsOfTypeAll<UniqueMeshesFinderWindow>();
            foreach (var window in windows) {
                window.Initialize();
            }
        }
    }
}
