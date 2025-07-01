using Awaken.TG.Editor.Graphics.LeshyRenderer;
using Awaken.TG.Editor.Utility;
using Awaken.TG.LeshyRenderer;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.ToolbarTools {
    [Overlay(typeof(SceneView), "Leshy scene view toolbar", defaultDisplay = true,
        defaultDockPosition = DockPosition.Top, defaultDockZone = DockZone.LeftColumn, defaultDockIndex = -1)]
    public class LeshySceneViewToolbar : Overlay {
        bool _isLeshyEnabled;
        Label _systemNameLabel;
        Button _switchToLeshyButton, _switchToVSPButton, _bakeButton;
        LeshyManager _leshy;
        // VegetationSystemPro _vsp;
        GameObject _vegetationGO;
        bool FoundLeshyAndVSP => _leshy != null;// && _vsp != null;

        public override VisualElement CreatePanelContent() {
            UpdateLeshyAndVSPReferences();
            var root = CreateMenuElements();
            //When panel is opened for the first time, Unity calls AttachToPanelEvent
            //When panel is closed, Unity calls DetachFromPanelEvent.
            //When panel is dragged, Unity calls DetachFromPanelEvent and then immediately AttachToPanelEvent.
            root.RegisterCallback<AttachToPanelEvent>(OnShowPanel);
            root.RegisterCallback<DetachFromPanelEvent>(OnHidePanel);
            return root;
        }

        VisualElement CreateMenuElements() {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            _systemNameLabel = new Label();
            _systemNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            root.Add(_systemNameLabel);

            _switchToLeshyButton = new Button(SwitchToLeshy);
            _switchToLeshyButton.text = "Switch to Leshy";
            root.Add(_switchToLeshyButton);

            _switchToVSPButton = new Button(SwitchToVSP);
            _switchToVSPButton.text = "Switch to VSP";
            root.Add(_switchToVSPButton);

            _bakeButton = new Button(Bake);
            _bakeButton.text = "Full Bake";
            root.Add(_bakeButton);
            Refresh();

            return root;
        }

        void UpdateLeshyAndVSPReferences() {
            _leshy = Object.FindFirstObjectByType<LeshyManager>(FindObjectsInactive.Include);
            // _vsp = Object.FindFirstObjectByType<VegetationSystemPro>(FindObjectsInactive.Include);
            _vegetationGO = GameObject.Find("Vegetation");
        }

        void Refresh() {
            _systemNameLabel.text = GetActiveSystemName();
            GetUIElementsEnabledState(out bool showLabel, out bool switchToLeshy, out bool switchToVSP, out bool bake);
            ShowUIElement(_systemNameLabel, showLabel);
            ShowUIElement(_switchToLeshyButton, switchToLeshy);
            ShowUIElement(_switchToVSPButton, switchToVSP);
            ShowUIElement(_bakeButton, bake);
        }

        void SwitchToLeshy() {
            if (!_leshy.ForceDisable) {
                return;
            }

            // _vsp.ForceDisable = true;
            _leshy.EDITOR_SetDisabled(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ReenableVegetation();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            EditorUtility.SetDirty(_leshy);
            // EditorUtility.SetDirty(_vsp);
            Refresh();
        }

        void SwitchToVSP() {
            return;
            // if (!_vsp.ForceDisable) {
            //     return;
            // }

            _leshy.EDITOR_SetDisabled(true);
            // _vsp.ForceDisable = false;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ReenableVegetation();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            EditorUtility.SetDirty(_leshy);
            // EditorUtility.SetDirty(_vsp);
            Refresh();
        }

        void Bake() {
            if (!FoundLeshyAndVSP) {
                Log.Critical?.Error("Leshy and VSP are not loaded");
                return;
            }

            // var persistentVegetationStorage = _vsp.GetComponent<PersistentVegetationStorage>();
            // if (persistentVegetationStorage == null) {
            //     Log.Critical?.Error(
            //         $"{_vsp.gameObject.name} does not have {nameof(PersistentVegetationStorage)} component");
            //     return;
            // }
            // var mapScene = Object.FindAnyObjectByType<MapScene>(FindObjectsInactive.Exclude);
            // if (mapScene == null) {
            //     Log.Critical?.Error($"No {nameof(MapScene)} component in open scenes");
            //     return;
            // }
            // LeshyManagerEditor.FullBake(_leshy, _vsp, persistentVegetationStorage, mapScene);
            // Refresh();
        }

        void ShowUIElement(VisualElement element, bool show) {
            element.style.display = new StyleEnum<DisplayStyle>(show ? DisplayStyle.Flex : DisplayStyle.None);
        }

        async Awaitable ReenableVegetation() {
            _vegetationGO.SetActive(false);
            // _vsp.enabled = false;
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
            _vegetationGO.SetActive(true);
            // _vsp.enabled = true;
        }

        void GetUIElementsEnabledState(out bool showLabel, out bool switchToLeshy, out bool switchToVSP,
            out bool bake) {
            if (!FoundLeshyAndVSP) {
                showLabel = switchToLeshy = switchToVSP = bake = false;
                return;
            }

            showLabel = true;
            //If both systems are enabled or disabled, show buttons to choose which one to enable
            // if (_leshy.ForceDisable == _vsp.ForceDisable) {
            //     switchToLeshy = switchToVSP = true;
            //     bake = false;
            // }

            bake = true;
            if (_leshy.ForceDisable) {
                switchToLeshy = true;
                switchToVSP = false;
            } else {
                switchToLeshy = false;
                switchToVSP = true;
            }
        }

        string GetActiveSystemName() {
            if (!FoundLeshyAndVSP) {
                return string.Empty;
            }

            // if (_leshy.ForceDisable == _vsp.ForceDisable) {
            //     if (_leshy.ForceDisable == false) {
            //         return $"Both systems are enabled";
            //     } else {
            //         return $"Both systems are disabled";
            //     }
            // }

            return _leshy.ForceDisable ? "VSP active" : "Leshy active";
        }

        void OnSceneSaving(Scene scene, string path) {
            if (!FoundLeshyAndVSP) {
                return;
            }

            _systemNameLabel.style.color = _leshy.ForceDisable ? Color.red : Color.white;
        }

        void OnShowPanel(AttachToPanelEvent _) {
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneOpened += OnSceneLoaded;
            EditorSceneManager.sceneClosed += OnSceneUnloaded;
        }

        void OnHidePanel(DetachFromPanelEvent _) {
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneOpened -= OnSceneLoaded;
            EditorSceneManager.sceneClosed -= OnSceneUnloaded;
        }

        void OnSceneLoaded(Scene _, OpenSceneMode lsm) {
            if (!BuildSceneBaking.isBakingScenes) {
                UpdateLeshyAndVSPReferences();
                Refresh();
            }
        }

        void OnSceneUnloaded(Scene _) {
            if (!BuildSceneBaking.isBakingScenes) {
                UpdateLeshyAndVSPReferences();
                Refresh();
            }
        }
    }
}