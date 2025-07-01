using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using Awaken.Utility.Animations;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur.SoulsOverview {
    [UsesPrefab("CharacterSheet/WyrdArthur/" + nameof(VWyrdTalentTreeSlotUI))]
    public class VWyrdTalentTreeSlotUI : View<WyrdTalentTreeSlotUI> {
        [SerializeField] ButtonConfig talentSlot;
        [SerializeField] Image runeIcon;
        [SerializeField] TooltipPosition tooltipPositionLeft;
        [SerializeField] TooltipPosition tooltipPositionRight;

        WyrdArthurPower TalentTreeUI => Target.ParentModel;
        public ARButton Button => talentSlot.button;
        
        Tween _runeColorTween;
        Tween _lineColorTween;
        bool _isHovered;

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Talent.Events.TalentChanged, this, RefreshTalent);
            talentSlot.InitializeButton(OnClicked);
            talentSlot.button.OnHover += OnSlotHovered;
            talentSlot.button.OnSelected += OnSlotSelected;
        }

        protected override void OnMount() {
            SetupTalent();
            talentSlot.button.ClearAllOnClickAudioFeedback();
        }
        
        public void Focus() {
            World.Only<Focus>().Select(talentSlot.button);
        }

        public void ColorLine(ARColor color, float duration = UITweens.ColorChangeDuration) {
            
        }

        void OnSlotHovered(bool isHovering) {
            if (RewiredHelper.IsGamepad) return;
            Select(isHovering);
        }
        
        void OnSlotSelected(bool isSelected) {
            if (RewiredHelper.IsGamepad == false) return;
            Select(isSelected);
        }
        
        void OnClicked() {
            talentSlot.button.PlayClickAudioFeedback(Target.Talent.CanBeUpgraded && TalentTree.IsUpgradeAvailable, false);
        }

        void Select(bool state) {
            TalentTreeUI.SelectTalent(Target, state);
            ShowTooltipOnHover(state);
        }
        
        void ShowTooltipOnHover(bool hover) {
            _isHovered = hover;
            var tooltip = Target.ParentModel.Tooltip;
            
            if (hover) {
                tooltip.SetPosition(tooltipPositionLeft, tooltipPositionRight);
                string currentDescription = Target.Talent.IsUpgraded ? Target.Talent.CurrentLevelDescription : string.Empty;
                string nextDescription = Target.Talent.MaxLevelReached ? string.Empty : Target.Talent.NextLevelDescription;
                tooltip.Show(Target.Talent.TalentName, currentDescription, nextDescription);
            } else {
                tooltip.Hide();
            }
        }
        
        void SetupTalent() {
            bool active = Target.IsUpgraded;
            _runeColorTween = runeIcon.DOGraphicColor(active ? ARColor.DarkerMainAccent : ARColor.MainGrey, UITweens.ColorChangeDuration);
            ColorLine(active ? ARColor.DarkerMainAccent : ARColor.MainGrey);
        }

        void RefreshTalent() {
            SetupTalent();
            
            if (_isHovered) {
                string currentDescription = Target.Talent.IsUpgraded ? Target.Talent.CurrentLevelDescription : string.Empty;
                string nextDescription = Target.Talent.MaxLevelReached ? string.Empty : Target.Talent.NextLevelDescription;
                Target.ParentModel.Tooltip.Refresh(Target.Talent.TalentName, currentDescription, nextDescription);
            }
        }

        protected override IBackgroundTask OnDiscard() {
            UITweens.DiscardTween(ref _runeColorTween);
            UITweens.DiscardTween(ref _lineColorTween);
            return base.OnDiscard();
        }
    }
}