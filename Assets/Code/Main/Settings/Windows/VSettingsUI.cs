using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Rewired;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Windows {
    [UsesPrefab("Settings/" + nameof(VSettingsUI))]
    public class VSettingsUI : View<AllSettingsUI>, IAutoFocusBase {
        public Transform settingsParent;
        public RectTransform promptsHost;

        [Title("Tabs settings")]
        [SerializeField] Color normalTextColor;
        [SerializeField] Color selectedTextColor;
        [SerializeField] float animationTime;

        public SettingsTabButton[] Tabs { get; private set; }
        public Transform Host => settingsParent;
        public Transform PromptsHost => promptsHost;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            // tab buttons
            Tabs = GetComponentsInChildren<SettingsTabButton>();
            Tabs.ForEach(SetupTab);
            Target.ListenTo(Model.Events.AfterChanged, UpdateTabs, this);
            World.Only<Focus>().ListenTo(Focus.Events.ControllerChanged, OnControllerChanged, this);
            UpdateTabs();

            Target.AddElement(new TabsSwitcher(Tabs, true));
        }

        void Update() {
            World.Only<GraphicPresets>().RefreshActivePreset();
        }
        
        void OnControllerChanged(ControllerType controller) {
            if (Target.CurrentTabType == SettingsTabType.ControlsSettings || Target.CurrentTabType == SettingsTabType.GamepadControlsSettings) {
                Target.ReplaceContent(SettingsTabType.GraphicSettings);
            } else {
                UpdateTabs();
            }
        }
        
        // === Update View
        void UpdateTabs() {
            Tabs.ForEach(t => t.gameObject.SetActive(ShouldBeActive(t)));
            if (Target.CurrentTabType == null) {
                return;
            }

            SettingsTabButton currentTab = Tabs.FirstOrDefault(t => t.IsActive);
            SettingsTabButton shouldBeActiveTab = Tabs.FirstOrDefault(t => t.Type == Target.CurrentTabType);

            if (currentTab != shouldBeActiveTab) {
                SettingsTabButton.SwitchTab(currentTab, shouldBeActiveTab);
            }
        }
        
        // === Helpers
        bool ShouldBeActive(SettingsTabButton tab) {
            if (tab.Type == SettingsTabType.ControlsSettings) {
                return !RewiredHelper.IsGamepad;
            }

            if (tab.Type == SettingsTabType.GamepadControlsSettings) {
                return RewiredHelper.IsGamepad;
            }

            if (tab.Type == SettingsTabType.DisplaySettings && PlatformUtils.IsConsole) {
                return false;
            }

            return true;
        }
        
        void SetupTab(SettingsTabButton tab) {
            EventReference tabChangeSound = CommonReferences.Get.AudioConfig.TabSelectedSound;
            tab.Setup(normalTextColor, selectedTextColor, animationTime);
            tab.OnClick.AddListener(() => {
                Target.ReplaceContent(tab.Type);
                FMODManager.PlayOneShot(tabChangeSound);
            });
        }

        protected override IBackgroundTask OnDiscard() {
            if (Target.ShouldDelayViewDiscard) {
                return new BackgroundUniTask(UniTask.DelayFrame(5));
            } else {
                return base.OnDiscard();
            }
        }
    }
}