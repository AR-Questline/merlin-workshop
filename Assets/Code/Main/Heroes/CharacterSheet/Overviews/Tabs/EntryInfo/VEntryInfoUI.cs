using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo {
    [UsesPrefab("CharacterSheet/Overview/VEntryInfoUI")]
    public class VEntryInfoUI : FoldingViewUI<EntryInfoUI> {
        const float TextTweenDuration = 0.15f;
        
        [SerializeField] TextMeshProUGUI infoText;
        [SerializeField] float heightAllowance = 20f;

        public override Transform DetermineHost() => Target.ParentModel.View<IVEntryParentUI>().EntriesParent;
        protected override float PreferredHeight { get; set; }

        protected override void OnInitialize() {
            base.OnInitialize();
            infoText.SetText(Target.entryDescription);
            infoText.DOFade(0f, 0f).SetUpdate(true);
        }

        public override Sequence Fold() {
            if (string.IsNullOrEmpty(Target.entryDescription)) {
                return null;
            }
            
            PreferredHeight = infoText.preferredHeight + heightAllowance;
            
            _showSequence = base.Fold()
                .AppendInterval(SequenceDelay)
                .Join(infoText.DOFade(1f, TextTweenDuration));
            return _showSequence;
        }

         public override Sequence Unfold() {
            if (string.IsNullOrEmpty(Target.entryDescription)) {
                return null;
            }
            
            _showSequence.Kill();
            _hideSequence.Kill();
            _hideSequence = base.Unfold()
                .Join(infoText.DOFade(0f, TextTweenDuration));
            return _hideSequence;
         }
    }
}