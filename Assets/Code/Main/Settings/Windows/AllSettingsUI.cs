using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Windows {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VSettingsUI))]
    public partial class AllSettingsUI : Model, ISettingHolder, IClosable, IUIStateSource, IPromptHost {
        const string LastSettingsTab = "Last_All_Settings_UI_Tab";
        
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        // === State
        static ContextualFacts Facts => Services.TryGet<GameplayMemory>()?.Context();
        readonly List<IVSetting> _spawnedSettings = new();
        PopupUI _popup;

        public bool ShouldDelayViewDiscard { get; private set; }
        public SettingsTabType CurrentTabType { get; private set; }
        public Transform PromptsHost => View<VSettingsUI>().PromptsHost;
        public Prompts Prompts => Element<Prompts>();
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown).WithPauseTime().WithShortcutLayer();
        static SettingsMaster SettingsMaster => World.Only<SettingsMaster>();

        // === Initialization
        protected override void OnInitialize() {
            World.Services.Get<FpsLimiter>().RegisterLimit(this, FpsLimiter.DefaultUIFpsLimit);
            this.ListenTo(Model.Events.AfterFullyInitialized, SpawnSettings, this);
        }

        protected override void OnFullyInitialized() {
            var prompts = AddElement(new Prompts(this));
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Settings.RestoreDefaults, LocTerms.SettingsRestoreDefaults.Translate(), RestoreDefaults, Prompt.Position.Last), this);
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Settings.ApplyChanges, LocTerms.SettingsApply.Translate(), Apply, Prompt.Position.Last), this)
                .AddAudio(new PromptAudio { TapSound = CommonReferences.Get.AudioConfig.ButtonApplySound });
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), Close, Prompt.Position.Last), this);
        }

        void SpawnSettings() {
            int lastTabIndex = World.HasAny<TitleScreenUI>() ? 0 : Facts?.Get<int>(LastSettingsTab) ?? 0;
            ReplaceContent(View<VSettingsUI>().Tabs[lastTabIndex].Type);
        }

        // === Operations
        void Apply() {
            ShouldDelayViewDiscard = World.Only<UpScaling>().Options.Any(o => o.WasChanged) || World.Only<Vegetation>().Options.Any(o => o.WasChanged);
            World.Only<SettingsMaster>().Apply();
            Discard();
        }

        void RestoreDefaults() {
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.RestoreDefaults);
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupDefaultSettings.Translate(),
                PopupUI.AcceptTapPrompt(() => {
                    var settings = CurrentTabType.GetSettings(SettingsMaster);
                    if (settings != null) {
                        World.Only<SettingsMaster>().RestoreDefaults(settings);
                    }
                    DiscardPopup();
                }),
                PopupUI.CancelTapPrompt(DiscardPopup),
                LocTerms.PopupDefaultSettingsTitle.Translate()
            );
        }

        public void Close() {
            bool isDuringRebinding = World.Any<AllSettingsUI>()?.View<VNewKeyBinding>() ?? false;

            if (isDuringRebinding) {
                return;
            }
            
            if (_popup != null) {
                DiscardPopup();
                return;
            }
            
            bool anySettingNotApplied = SettingsMaster.Settings.Any(s => s.Options.Any(o => o.WasChanged));
            if (anySettingNotApplied) {
                _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                    LocTerms.PopupNotAppliedSettings.Translate(),
                    PopupUI.AcceptTapPrompt(ApplyAndClose).AddAudio(new PromptAudio { TapSound = CommonReferences.Get.AudioConfig.ButtonApplySound }),
                    PopupUI.CancelTapPrompt(CancelAndClose),
                    LocTerms.PopupNotAppliedSettingsTitle.Translate()
                );
            } else {
                CancelAndClose();
            }
        }

        public void ReplaceContent(SettingsTabType tabType) {
            if (CurrentTabType == tabType) return;
            CurrentTabType = tabType;
            var settings = tabType.GetSettings(SettingsMaster);
            if (settings != null) {
                SpawnSettingsViews(settings);
            }
            TriggerChange();
            Facts?.Set(LastSettingsTab, View<VSettingsUI>().Tabs.IndexOf(t => t.Type == tabType));
        }
        
        // === Helpers
        void CancelAndClose() {
            DiscardPopup();
            World.Only<SettingsMaster>().Cancel();
            Discard();
        }

        void ApplyAndClose() {
            DiscardPopup();
            Apply();
        }

        void DiscardPopup() {
            _popup?.Discard();
            _popup = null;
        }

        void SpawnSettingsViews(IEnumerable<ISetting> settings) {
            _spawnedSettings.ForEach(s => s.Discard());
            _spawnedSettings.Clear();

            foreach (var setting in settings) {
                SettingsUtil.SpawnViews(this, setting, View<VSettingsUI>().Host, _spawnedSettings);
            }

            // Create navigation between settings
            Selectable previous = null;
            foreach (var setting in _spawnedSettings) {
                previous = SettingsUtil.EstablishNavigation(previous, setting.MainSelectable);
            }

            // Cycle navigation
            Selectable firstSelectable = _spawnedSettings.FirstOrDefault(s => s.MainSelectable != null)?.MainSelectable;
            Selectable lastSelectable = _spawnedSettings.LastOrDefault(s => s.MainSelectable != null)?.MainSelectable;
            if (lastSelectable != null && firstSelectable != null) {
                lastSelectable.ChangeNavi(n => {
                    n.selectOnDown = firstSelectable;
                    return n;
                });
                firstSelectable.ChangeNavi(n => {
                    n.selectOnUp = lastSelectable;
                    return n;
                });
            }
            
            // focus first selectable
            World.Only<Focus>().Select(firstSelectable);
        }
    }
}