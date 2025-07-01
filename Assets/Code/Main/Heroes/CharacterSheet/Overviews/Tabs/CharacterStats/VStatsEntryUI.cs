using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats {
    [UsesPrefab("CharacterSheet/Stats/RPGStats/" + nameof(VStatsEntryUI))]
    public class VStatsEntryUI : View<StatsEntryUI>, IVEntryParentUI {
        [SerializeField] ButtonConfig statButtonConfig;
        [SerializeField] ButtonConfig plusButtonConfig;
        [SerializeField] ButtonConfig minusButtonConfig;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI statNameText;
        [SerializeField] TextMeshProUGUI statLevelText;
        [SerializeField] TextMeshProUGUI statChangeValueText;

        [SerializeField] Image plusRegularIcon;
        [SerializeField] Image plusFadeOutIcon;
        [SerializeField] Image minusRegularIcon;
        [SerializeField] Image minusFadeOutIcon;
        [field: SerializeField] public Transform EntriesParent { get; private set; }
        
        public bool IsFolded { get; private set; }
        public ARButton Button => statButtonConfig.button;
        public override Transform DetermineHost() => Target.ParentModel.View<IVEntryParentUI>().EntriesParent;

        GameObject MinusButtonObject => minusButtonConfig.button.gameObject;
        GameObject PlusButtonObject => plusButtonConfig.button.gameObject;
        VCharacterStatsEntryInfoUI VEntryInfoUI => Target?.Element<CharacterStatsEntryInfoUI>()?.View<VCharacterStatsEntryInfoUI>();
        Stat HeroRPGStat => Target.heroRPGStat;
        
        protected override void OnInitialize() {
            Target.ParentModel.ListenTo(CharacterStatsUI.Events.NewCharacterStatsApplied, RefreshEntry, this);
            Target.ParentModel.ListenTo(CharacterStatsUI.Events.PointsToApplyChange, OnStatValueChanged, this);

            statNameText.SetText(HeroRPGStat.Type.DisplayName);
            InitEntry();
            
            if (Target.icon is { IsSet: true } statsIcon) {
                statsIcon.RegisterAndSetup(this, icon);
            }

            statButtonConfig.InitializeButton();
            statButtonConfig.button.OnHover += OnStatHovered;
            statButtonConfig.button.OnSelected += OnStatSelected;
            statButtonConfig.button.OnEvent += OnButtonEvent;
            
            plusButtonConfig.InitializeButton(Target.IncreaseStatValue);
            minusButtonConfig.InitializeButton(Target.DecreaseStatValue);
        }
        
        public void SelectionOnFold(bool fold) {
            statButtonConfig.SetSelection(fold);
        }
        
        UIResult OnButtonEvent(UIEvent evt) {
            switch (evt) {
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.IncreaseValue:
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.UI.Generic.IncreaseValue:
                    Target.IncreaseStatValue();
                    return UIResult.Accept;
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.DecreaseValue:
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.UI.Generic.DecreaseValue:
                    Target.DecreaseStatValue();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }
        
        void OnStatHovered(bool isHovered) {
            if (RewiredHelper.IsGamepad) return;
            Fold(isHovered);
        }
        
        void OnStatSelected(bool isSelected) {
            if (RewiredHelper.IsGamepad == false) return;
            Fold(isSelected);
        }

        void Fold(bool state) {
            IsFolded = state;

            if (HasBeenDiscarded) {
                return;
            }
            
            Target.ParentModel.Fold(VEntryInfoUI);
        }

        void RefreshEntry() {
            InitEntry();
        }

        void InitEntry() {
            statLevelText.SetText($"{Mathf.FloorToInt(HeroRPGStat.ModifiedValue)}");
            statChangeValueText.SetText(string.Empty);
            PlusButtonObject.SetActive(Target.CanIncrease);
            MinusButtonObject.SetActive(Target.CanIncrease);
            DisplayPlusMinusButtons();
        }

        void OnStatValueChanged() {
            DisplayPlusMinusButtons();
            statChangeValueText.SetText(Target.StatChangeValue > 0 ? $"+{Target.StatChangeValue}" : string.Empty);
            int baseStat = Mathf.FloorToInt(HeroRPGStat.ModifiedValue);
            int baseStatWithStatChange = baseStat + Target.StatChangeValue;
            statLevelText.SetText($"{(Target.StatChangeValue > 0 ? $"{baseStatWithStatChange}" : baseStat)}");
        }
        
        void DisplayPlusMinusButtons() {
            plusRegularIcon.gameObject.SetActive(Target.CanIncrease);
            plusFadeOutIcon.gameObject.SetActive(!Target.CanIncrease);
            minusRegularIcon.gameObject.SetActive(Target.CanDecrease);
            minusFadeOutIcon.gameObject.SetActive(!Target.CanDecrease);
        }
    }
}