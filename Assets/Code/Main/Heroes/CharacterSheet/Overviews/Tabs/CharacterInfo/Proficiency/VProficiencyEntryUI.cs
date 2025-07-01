using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.Proficiency {
    [UsesPrefab("CharacterSheet/Overview/" + nameof(VProficiencyEntryUI))]
    public class VProficiencyEntryUI : FoldingViewUI<ProficiencyEntryUI>, IVEntryParentUI, ISemaphoreObserver {
        [SerializeField] ButtonConfig profButtonConfig;
        [SerializeField] TextMeshProUGUI proficiencyName;
        [SerializeField] TextMeshProUGUI proficiencyLevel;
        [SerializeField] Image proficiencyProgress;
        [SerializeField] Image proficiencyIcon;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] CanvasGroup contentCanvasGroup;
        [SerializeField] float preferredHeight = 58f;
        [field: SerializeField] public Transform EntriesParent { get; private set; }

        FragileSemaphore _isToggle;
        bool _hoveringSuppressed;
        
        Hero Hero => Hero.Current;
        ProficiencyStats ProficiencyStats => Hero.ProficiencyStats;
        VEntryInfoUI VEntryInfoUI => Target.Element<EntryInfoUI>().View<VEntryInfoUI>();

        protected override float PreferredHeight {
            get => preferredHeight;
            set => preferredHeight = value;
        }
        
        public override Transform DetermineHost() => Target.ParentModel.View<VProficiencyCategoryUI>().ContentHost;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            _isToggle = new FragileSemaphore(false, this, 0f, true);
            profButtonConfig.button.Interactable = false;
            profButtonConfig.button.OnClick += OnStatSelect;
            profButtonConfig.InitializeButton();
            proficiencyName.SetText(Target.proficiencyStat.DisplayName);
            proficiencyLevel.SetText(Hero.Stat(Target.proficiencyStat).ModifiedInt.ToString());
            proficiencyProgress.fillAmount = ProficiencyStats.GetProgressToNextLevel(Target.proficiencyStat);
            canvasGroup.DOFade(0f, 0f).SetUpdate(true);
            SuppressHovering(true);

            if (Target.proficiencyIcon is { IsSet: true } icon) {
                icon.RegisterAndSetup(this, proficiencyIcon);
            }
        }
        
        public override Sequence Fold() {
            SuppressHovering(false);
            _showSequence = base.Fold()
                .Join(canvasGroup.DOFade(1f, ShowDuration))
                .AppendInterval(SequenceDelay)
                .Join(contentCanvasGroup.DOFade(1f, ShowDuration));
            return _showSequence;
        }
        
        public override Sequence Unfold() {
            SuppressHovering(true);
            _isToggle.Set(false);
            _hideSequence = base.Unfold()
                .Join(canvasGroup.DOFade(0f, ShowDuration))
                .Join(contentCanvasGroup.DOFade(0f, ShowDuration));
            return _hideSequence;
        }

        void Update() {
            if (!_hoveringSuppressed) {
                _isToggle.Update();
            }
        }

        void OnStatSelect() {
            _isToggle.Set(!_isToggle.DesiredState);
        }

        void SuppressHovering(bool suppress) {
            _hoveringSuppressed = suppress;
            profButtonConfig.button.Interactable = !suppress;
        }

        void ToggleOn() {
            VEntryInfoUI.Fold();
            profButtonConfig.SetSelection(true);
        }
        
        void ToggleOff() {
            VEntryInfoUI.Unfold();
            profButtonConfig.SetSelection(false);
        }
        
        void ISemaphoreObserver.OnUp() => ToggleOn();
        void ISemaphoreObserver.OnDown() => ToggleOff();
    }
}