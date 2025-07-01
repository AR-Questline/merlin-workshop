using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Entries {
    [SpawnsView(typeof(VJournalCategoryDropdownUI))]
    public partial class JournalCategoryDropdownUI : Element<IJournalCategoryUI> {
        public sealed override bool IsNotSaved => true;

        Sequence _unfoldingSequence;
        Sequence _foldingSequence;
        public bool IsFolded { get; private set; }
        public string CategoryName { get; }
        
        public JournalCategoryDropdownUI(string categoryName) {
            CategoryName = categoryName;
            IsFolded = true;
        }
        
        public void ToggleCategory() {
            if (IsFolded) {
                UnfoldCategory();
            } else {
                FoldCategory();
            }
            
            View<VJournalCategoryDropdownUI>().SetArrow();
            View<VJournalCategoryDropdownUI>().FocusCategory();
        }

        public async UniTaskVoid ToggleCategoryAsync() {
            if (await AsyncUtil.DelayFrame(this, 2)) {
                ToggleCategory();
            }
        }

        void UnfoldCategory() {
            IsFolded = false;
            _unfoldingSequence.Kill();
            _unfoldingSequence = DOTween.Sequence().SetUpdate(true);
            foreach (IModel child in Elements<JournalButtonEntryUI>()) {
                _unfoldingSequence.Join(child.View<IFoldingViewUI>().Fold());
            }
        }
        
        void FoldCategory() {
            IsFolded = true;
            _foldingSequence.Kill();
            _foldingSequence = DOTween.Sequence().SetUpdate(true);
            foreach (IModel child in Elements<JournalButtonEntryUI>()) {
                _foldingSequence.Join(child.View<IFoldingViewUI>().Unfold());
            }
        }
    }
}