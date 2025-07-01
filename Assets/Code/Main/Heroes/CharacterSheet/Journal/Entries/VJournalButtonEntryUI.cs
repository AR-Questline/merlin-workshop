using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Entries {
    [UsesPrefab("CharacterSheet/Journal/" + nameof(VJournalButtonEntryUI))]
    public class VJournalButtonEntryUI : FoldingViewUI<JournalButtonEntryUI> {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Image icon;
        [SerializeField] GameObject separator;
        [SerializeField] CanvasGroup contentCanvasGroup;
        [SerializeField] CanvasGroup canvasGroup;
        
        public override Transform DetermineHost() => Target.ParentModel.View<IVEntryParentUI>().EntriesParent;
        protected override float PreferredHeight { get; set; } = 126f;

        protected override void OnInitialize() {
            base.OnInitialize();
            canvasGroup.alpha = 0;
            nameLabel.SetText(Target.Data.Name);

            bool imageIsSet = Target.Data.PreviewImage is { IsSet: true };
            icon.TrySetActiveOptimized(imageIsSet);
            separator.SetActiveOptimized(imageIsSet);
            
            if (imageIsSet) {
                Target.Data.PreviewImage?.RegisterAndSetup(this, icon);
            }

            buttonConfig.InitializeButton(Select);
            World.EventSystem.ListenTo(EventSelector.AnySource, IJournalCategoryUI.Events.EntrySelected, this, SetSelection);
        }

        void Select() {
            Target.ParentModel.ParentModel.SelectEntry(Target.Data);
        }

        void SetSelection(IJournalEntryData entryData) {
            buttonConfig.SetSelection(entryData.Equals(Target.Data));
        }
        
        public override Sequence Fold() {
            _showSequence = base.Fold()
                .JoinCallback(() => buttonConfig.button.Interactable = true)
                .Join(canvasGroup.DOFade(1f, ShowDuration))
                .AppendInterval(SequenceDelay)
                .Join(contentCanvasGroup.DOFade(1f, ShowDuration));
            return _showSequence;
        }
        
        public override Sequence Unfold() {
            _hideSequence = base.Unfold()
                .JoinCallback(() => buttonConfig.button.Interactable = false)
                .Join(canvasGroup.DOFade(0f, ShowDuration))
                .Join(contentCanvasGroup.DOFade(0f, ShowDuration));
            return _hideSequence;
        }
    }
}