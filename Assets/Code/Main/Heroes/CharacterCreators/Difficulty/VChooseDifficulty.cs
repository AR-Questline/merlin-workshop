using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Difficulty {
    [UsesPrefab("CharacterCreator/Difficulty/" + nameof(VChooseDifficulty))]
    public class VChooseDifficulty : View<ChooseDifficulty>, IPromptHost, IAutoFocusBase {
        [SerializeField] TMP_Text title;
        [SerializeField] Transform promptHost;
        
        [Title("Mode")]
        [SerializeField] ButtonConfig modeButton;
        [SerializeField] GameObject modeSection;
        [SerializeField] TMP_Text modeInfo;
        [SerializeField] VGenericPromptUI storyModePromptUI;
        [SerializeField] VGenericPromptUI survivalModePromptUI;
        [SerializeField] TMP_Text modeDescription;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        public Transform PromptsHost => promptHost;
        public VGenericPromptUI StoryModePromptUI => storyModePromptUI;
        public VGenericPromptUI SurvivalModePromptUI => survivalModePromptUI;

        VCDifficultyPreset[] _difficultyPresets;

        protected override void OnMount() {
            _difficultyPresets = GetComponentsInChildren<VCDifficultyPreset>();

            SetupView();
            FocusFirst();
        }
        
        void SetupView() {
            title.SetText(LocTerms.SettingsDifficulty.Translate());
            modeInfo.SetText(LocTerms.AdditionalSettings.Translate());
            
            modeSection.SetActiveOptimized(false);
            modeButton.InitializeButton(Target.ToggleMode);
            Target.ListenTo(ChooseDifficulty.Events.DifficultyModeToggled, OnModeToggled, this);
        }
        
        void FocusFirst() {
            var firstPreset = _difficultyPresets.FirstOrDefault();
            if (firstPreset != null) {
                World.Only<Focus>().Select(firstPreset.Button);
            }
        }
        
        void OnModeToggled(bool isToggle) {
            modeButton.SetSelection(isToggle);
        }

        public void SetupModeSection(bool hasMode, string modeDesc) {
            modeButton.SetSelection(false);
            modeDescription.TrySetText(modeDesc);
            modeSection.SetActiveOptimized(hasMode);
        }
    }
}