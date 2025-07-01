using Awaken.TG.Main.Grounds;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEditor.Toolbars;

namespace Awaken.TG.Editor.ToolbarTools {
    [Overlay(typeof(SceneView), "Snap to ground", defaultDisplay = true,
        defaultDockPosition = DockPosition.Top, defaultDockZone = DockZone.LeftColumn, defaultDockIndex = 1)]
    public class SnapToGroundToolbar : Overlay {
        const int Margins = 4;
        static readonly string EnabledEditorPrefsKey = $"{nameof(SnapToGroundToolbar)};{nameof(ConstantSnapEnabled)}";
        static readonly string AlignmentEnabledEditorPrefsKey = $"{nameof(SnapToGroundToolbar)};{nameof(AlignmentEnabled)}";
        static readonly string SnapAnythingEnabledEditorPrefsKey = $"{nameof(SnapToGroundToolbar)};{nameof(SnapAnythingEnabled)}";

        VisualElement _root;
        VisualElement _headerToggles;
        EditorToolbarToggle _enableToggle;
        EditorToolbarToggle _alignmentToggle;
        EditorToolbarToggle _snapAnythingToggle;
        Label _statusLabel;
        Label _warningLabel;
        Label _tipLabel;

        Transform _currentGroundedObj;
        Transform _lastGroundedObj;
        Transform _alwaysSnapObject;

        static bool ConstantSnapEnabled { get; set; }
        static bool AlignmentEnabled { get; set; }
        static bool SnapAnythingEnabled { get; set; }
        static SnapToGroundToolbar Instance { get; set; }

        public override VisualElement CreatePanelContent() {
            ConstantSnapEnabled = EditorPrefs.GetBool(EnabledEditorPrefsKey, false);
            AlignmentEnabled = EditorPrefs.GetBool(AlignmentEnabledEditorPrefsKey, false);
            SnapAnythingEnabled = EditorPrefs.GetBool(SnapAnythingEnabledEditorPrefsKey, false);
                
            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Column;
            
            _headerToggles = new VisualElement();
            _headerToggles.style.flexDirection = FlexDirection.Row;
            _root.Add(_headerToggles);
            
            AddEnableToggle();
            AddAlignmentToggle();
            AddSnapAnythingToggle();
            AddStatusLabel();
            AddWarningLabel();
            AddTipLabel();

            _root.RegisterCallback<AttachToPanelEvent>(OnShow);
            _root.RegisterCallback<DetachFromPanelEvent>(OnHide);

            RefreshView();

            Instance = this;
            return _root;
        }
        
        public static void AssignObjectAlwaysSnapping(Transform obj) {
            if (Instance != null) {
                Instance._alwaysSnapObject = obj;
            }
        }

        [Shortcut("Tools/Toggle ground snapping", KeyCode.G)]
        static void ToggleGroundSnapping() {
            ConstantSnapEnabled = !ConstantSnapEnabled;
            Instance?.RefreshView();
            EditorPrefs.SetBool(EnabledEditorPrefsKey, ConstantSnapEnabled);
        }

        [Shortcut("Tools/Snap selected object to ground", KeyCode.G, ShortcutModifiers.Shift)]
        static void SnapToGroundSelected() {
            Transform selected = Selection.activeObject?.GetComponent<Transform>();
            if (selected) {
                Undo.RecordObject(selected, $"{nameof(SnapToGroundToolbar)};{nameof(SnapToGroundSelected)}");
                Ground.SnapToGroundSafe(selected, AlignmentEnabled ? AlignMode.GroundNormal : AlignMode.Up);
                PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
            }
        }

        [Shortcut("Tools/Rotate selected object randomly", KeyCode.R, ShortcutModifiers.Shift)]
        static void RotateRandomlySelected() {
            Transform selected = Selection.activeObject?.GetComponent<Transform>();
            if (selected) {
                Undo.RecordObject(selected, $"{nameof(SnapToGroundToolbar)};{nameof(RotateRandomlySelected)}");
                selected.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);
                PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
            }
        }

        void AddEnableToggle() {
            _enableToggle = new EditorToolbarToggle("Constant Snap") {
                value = ConstantSnapEnabled
            };
            _enableToggle.RegisterCallback((MouseUpEvent evt) => {
                ConstantSnapEnabled = !ConstantSnapEnabled;
                EditorPrefs.SetBool(EnabledEditorPrefsKey, ConstantSnapEnabled);
                RefreshView();
            });
            _enableToggle.style.marginBottom = Margins;
            _headerToggles.Add(_enableToggle);
        }

        void AddAlignmentToggle() {
            _alignmentToggle = new EditorToolbarToggle("Alignment") {
                value = AlignmentEnabled
            };
            _alignmentToggle.RegisterCallback((MouseUpEvent evt) => {
                AlignmentEnabled = !AlignmentEnabled;
                EditorPrefs.SetBool(AlignmentEnabledEditorPrefsKey, AlignmentEnabled);
                RefreshView();
            });
            _alignmentToggle.style.marginBottom = Margins;
            _root.Add(_alignmentToggle);
        }

        void AddSnapAnythingToggle() {
            _snapAnythingToggle = new EditorToolbarToggle("Snap anything") {
                value = SnapAnythingEnabled
            };
            _snapAnythingToggle.RegisterCallback((MouseUpEvent evt) => {
                SnapAnythingEnabled = !SnapAnythingEnabled;
                EditorPrefs.SetBool(SnapAnythingEnabledEditorPrefsKey, SnapAnythingEnabled);
                RefreshView();
            });
            _snapAnythingToggle.style.marginBottom = Margins;
            _snapAnythingToggle.style.marginLeft = Margins;
            _headerToggles.Add(_snapAnythingToggle);
        }

        void AddStatusLabel() {
            _statusLabel = new Label();
            _statusLabel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            _root.Add(_statusLabel);
        }

        void AddTipLabel() {
            _tipLabel = new Label(GetTipText());
            _tipLabel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            _root.Add(_tipLabel);
        }

        void AddWarningLabel() {
            _warningLabel = new Label();
            _warningLabel.style.marginTop = _warningLabel.style.marginBottom = Margins;
            _warningLabel.style.color = Color.yellow;
            _root.Add(_warningLabel);
        }

        void OnShow(AttachToPanelEvent evt) {
            EditorApplication.update += Update;
        }

        void OnHide(DetachFromPanelEvent evt) {
            EditorApplication.update -= Update;
        }

        void Update() {
            if (!ConstantSnapEnabled || Application.isPlaying || !Selection.activeObject) {
                return;
            }

            if (Selection.activeObject is not GameObject selectedGO) {
                return;
            }

            Transform selectedGOTransform = selectedGO.transform;

            if (SnapAnythingEnabled || _alwaysSnapObject == selectedGOTransform) {
                _currentGroundedObj = selectedGOTransform;
            } else {
                _currentGroundedObj = selectedGO.GetComponent<ISnappable>()?.Transform;
            }
            
            if (_currentGroundedObj != _lastGroundedObj) {
                RefreshView();
                _lastGroundedObj = _currentGroundedObj;
            }

            if (!_currentGroundedObj) {
                return;
            }

            var scene = Physics.defaultPhysicsScene;
            if (PrefabStageUtility.GetCurrentPrefabStage()?.mode == PrefabStage.Mode.InIsolation) {
                scene = PrefabStageUtility.GetCurrentPrefabStage().scene.GetPhysicsScene();
            }

            if (Ground.SnapToGroundSafe(_currentGroundedObj, AlignmentEnabled ? AlignMode.GroundNormal : AlignMode.Up, scene)) {
                EditorUtility.SetDirty(_currentGroundedObj);
            }
        }

        void RefreshView() {
            _enableToggle.value = ConstantSnapEnabled;

            _statusLabel.text = GetStatusText();
            _statusLabel.style.color = GetStatusColor();

            _warningLabel.text = GetWarningText();
            _warningLabel.style.display = GetWarningStyleDisplay();
        }

        string GetStatusText() {
            return ConstantSnapEnabled ? "Constant snap enabled!" : "Constant snap disabled!";
        }

        Color GetStatusColor() {
            return ConstantSnapEnabled ? Color.green : Color.red;
        }

        string GetWarningText() {
            return _currentGroundedObj == null ? "Make sure to select an object\nwith ISnappable component!" : "";
        }
        
        DisplayStyle GetWarningStyleDisplay() {
            return !SnapAnythingEnabled && _currentGroundedObj == null && ConstantSnapEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        string GetTipText() {
            return "G - toggle constant-snapping\n" +
                   "Shift + G - snap anything\n" +
                   "Shift + R - rotate randomly";
        }
    }
}