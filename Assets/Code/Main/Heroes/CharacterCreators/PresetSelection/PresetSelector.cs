using System.Collections.Generic;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection {
    [SpawnsView(typeof(VPresetSelector))]
    public partial class PresetSelector : Element<TitleScreenUI>, IPromptHost {
        public Transform PromptsHost => View<VPresetSelector>().PromptsHost;
        PresetSelectorConfig Config { get; set; }

        Prompts Prompts => Element<Prompts>();

        Dictionary<Region, SceneSets> _sceneSets;
        readonly Region _selectedRegion;
        
        Prompt _confirm;
        Prompt _back;
        PopupUI _popup;
        CharacterBuildPreset _selectedPreset;

        public PresetSelector(Region region) {
            _selectedRegion = region;
        }

        protected override void OnInitialize() {
            Config = CommonReferences.Get.presetSelectorConfig;
            _sceneSets = new Dictionary<Region, SceneSets> {
                {Region.Cuanacht, Config.Cuanacht},
                {Region.Forlorn, Config.Forlorn}
            };
        }

        protected override void OnFullyInitialized() {
            AddElement(new Prompts(this));
            SpawnPrompts();
            AsyncAbortOrToCamera().Forget();
            SpawnHeroPresets();
        }

        void SpawnHeroPresets() {
            if (Config == null) return;
            
            foreach (CharacterBuildPreset preset in _sceneSets[_selectedRegion].presets) {
                AddElement(new HeroPreset(preset));
            }
            
            World.Only<Focus>().Select(Element<HeroPreset>().View<VHeroPreset>().Button);
        }

        async UniTaskVoid AsyncAbortOrToCamera() {
            if (Config == null) {
                Log.Important?.Error("No PresetSelectorConfig found in CommonReferences", CommonReferences.Get);
                await AsyncUtil.DelayFrame(this);
                if (!HasBeenDiscarded) {
                    Discard();
                }
                return;
            }
            
            World.Services.Get<TransitionService>().ToCamera(1).Forget();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            RemovePrompts();
            ParentModel.View<VTitleScreenUI>().SwitchButtons(false);
            base.OnDiscard(fromDomainDrop);
        }
        
        public void SelectPreset(CharacterBuildPreset preset) {
            _selectedPreset = preset;
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupSelectPreset.Translate(),
                PopupUI.AcceptTapPrompt(StartWithPresetSelected),
                PopupUI.CancelTapPrompt(DiscardPopup),
                LocTerms.PopupSelectPresetTitle.Translate()
            );
        }
        
        void DiscardPopup() {
            _popup?.Discard();
            _popup = null;
        }
        
        public void StartWithPresetSelected() {
            DiscardPopup();
            Discard();
            StartGameData data = new() {
                sceneReference = _sceneSets[_selectedRegion].Scene,
                withHeroCreation = true,
                characterPresetData = _selectedPreset
            };
            TitleScreenUtils.StartNewGame(data);
        }

        // === Prompts
        void RemovePrompts() {
            Prompts.RemovePrompt(ref _confirm);
            Prompts.RemovePrompt(ref _back);
        }
        
        void SpawnPrompts() {
            _confirm = Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Confirm.Translate()), this);
            _back = Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), Discard), this);
        }
    }
}
