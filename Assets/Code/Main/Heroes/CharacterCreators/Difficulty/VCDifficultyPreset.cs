using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using ARDifficulty = Awaken.TG.Main.Settings.Gameplay.Difficulty;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Difficulty {
    [UsesPrefab("CharacterCreator/Difficulty/" + nameof(VCDifficultyPreset))]
    public class VCDifficultyPreset : ViewComponent<ChooseDifficulty> {
        [SerializeField, RichEnumExtends(typeof(ARDifficulty))] RichEnumReference difficulty;
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] TMP_Text description;
        
        [Title("Mode")]
        [SerializeField, RichEnumExtends(typeof(ARDifficulty)), CanBeNull] RichEnumReference modeDifficulty;

        public ARButton Button => buttonConfig.button;
        ARDifficulty Difficulty => difficulty.EnumAs<ARDifficulty>();
        bool _isSelected;

        protected override void OnAttach() {
            Target.ListenTo(ChooseDifficulty.Events.DifficultySelected, Refresh, this);
            buttonConfig.InitializeButton(SelectPreset, Difficulty.Name);
            buttonConfig.button.OnHover += OnHover;
            buttonConfig.button.OnSelected += OnSelect;
            description.SetActiveAndText(false, Difficulty.Description);
        }
        
        void SelectPreset() {
            Target.SelectPreset(Difficulty, modeDifficulty != null ? modeDifficulty.EnumAs<ARDifficulty>() : null);
        }
        
        void OnHover(bool state) {
            if (RewiredHelper.IsGamepad) return;
            description.TrySetActiveOptimized(state || _isSelected);
        }
            
        void OnSelect(bool state) {
            if (RewiredHelper.IsGamepad == false) return;
            description.TrySetActiveOptimized(state || _isSelected);
        }

        void Refresh(ARDifficulty difficulty) {
            if (difficulty == null) {
                return;
            }
            
            _isSelected = difficulty == Difficulty;
            buttonConfig.SetSelection(_isSelected);
            description.TrySetActiveOptimized(_isSelected);
        }
    }
}