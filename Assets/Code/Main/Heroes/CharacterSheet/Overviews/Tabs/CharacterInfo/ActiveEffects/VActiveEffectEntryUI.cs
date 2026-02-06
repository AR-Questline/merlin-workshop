using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.ActiveEffects {
    [UsesPrefab("CharacterSheet/Overview/" + nameof(VActiveEffectEntryUI))]
    public class VActiveEffectEntryUI : View<ActiveEffectEntryUI>, IVEntryParentUI {
        [SerializeField] Image effectIcon;
        [SerializeField] ButtonConfig effectButtonConfig;
        [SerializeField] TextMeshProUGUI effectNameText;
        [SerializeField] TextMeshProUGUI stackLvl;
        [SerializeField] TextMeshProUGUI durationText;
        [SerializeField] Image buffBackground;
        [SerializeField] Image debuffBackground;
        [SerializeField] Image buildupBackground;
        [SerializeField] Image buffFrame;
        [SerializeField] Image debuffFrame;
        [SerializeField] Image buildupFrame;
        [field: SerializeField] public Transform EntriesParent { get; private set; }
        
        VEffectEntryInfoUI VEntryInfoUI => Target.TryGetElement<EntryInfoUI>()?.View<VEffectEntryInfoUI>();
        Status HeroActiveStatus => Target.heroStatus;
        
        public override Transform DetermineHost() => Target.ParentModel.View<IVEntryParentUI>().EntriesParent;
        bool _isToggle;

        protected override void OnInitialize() {
            if (HeroActiveStatus.SourceInfo.Icon is {IsSet: true}) {
                HeroActiveStatus.SourceInfo.Icon.RegisterAndSetup(this, effectIcon);
            }

            if (HeroActiveStatus.CanStack || HeroActiveStatus.StackLevel > 0) {
                stackLvl.SetActiveAndText(true, $"{HeroActiveStatus.StackLevel + 1}");
            } else {
                stackLvl.TrySetActiveOptimized(false);
            }
            
            effectNameText.SetText(HeroActiveStatus.SourceInfo.DisplayNameString);
            
            var timeDuration = HeroActiveStatus.TryGetElement<StatusDuration>()?.TryGetElement<TimeDuration>();
            if (timeDuration is { IsInfinite: false }) {
                durationText.SetText(timeDuration.DisplayText);
            } else if (HeroActiveStatus is BuildupStatus buildupStatus) {
                durationText.SetText(buildupStatus.DurationText);
                buildupBackground.TrySetActiveOptimized(true);
                buildupFrame.TrySetActiveOptimized(true);
            } else {
                durationText.SetText("");
            }
            
            bool isDebuff = HeroActiveStatus.Type == StatusType.Debuff 
                            || HeroActiveStatus.Type == StatusType.Curse 
                            || HeroActiveStatus.Type == StatusType.Sin;
            debuffBackground.TrySetActiveOptimized(isDebuff);
            debuffFrame.TrySetActiveOptimized(isDebuff);
            buffBackground.TrySetActiveOptimized(!isDebuff);
            buffFrame.TrySetActiveOptimized(!isDebuff);
            
            effectButtonConfig.InitializeButton(OnEffectSelect);
        }

        void OnEffectSelect() {
            _isToggle = !_isToggle;
            
            if (_isToggle) {
                VEntryInfoUI.Fold();
                effectButtonConfig.SetSelection(true);
            } else {
                VEntryInfoUI.Unfold();
                effectButtonConfig.SetSelection(false);
            }
        }
    }
}