using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using DG.Tweening;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.Proficiency {
    [SpawnsView(typeof(VProficiencyCategoryUI))]
    public partial class ProficiencyCategoryUI : Element<ProficienciesUI> {
        Sequence _unfoldingSequence;
        Sequence _foldingSequence;
        
        readonly List<ProfStatType> _proficiencies;

        public readonly string categoryName;
        public readonly ShareableSpriteReference categoryIcon;

        public bool IsFolded { get; private set; } = true;
        
        public ProficiencyCategoryUI(string categoryName, ShareableSpriteReference categoryIcon, List<ProfStatType> proficiencies) {
            this.categoryName = categoryName;
            this.categoryIcon = categoryIcon;
            this._proficiencies = proficiencies;
        }

        public void SpawnProficiencies() {
            foreach (ProfStatType profStatType in _proficiencies) {
                var proficiencyEntry = new ProficiencyEntryUI(profStatType);
                AddElement(proficiencyEntry);
            }
        }
        
        public void ToggleCategory() {
            if (IsFolded) {
                UnfoldCategory();
            } else {
                FoldCategory();
            }
        }

        void UnfoldCategory() {
            IsFolded = false;
            _unfoldingSequence.Kill();
            _unfoldingSequence = DOTween.Sequence().SetUpdate(true);
            foreach (IModel child in Elements<ProficiencyEntryUI>()) {
                _unfoldingSequence.Join(child.View<IFoldingViewUI>().Fold());
            }
        }
        
        void FoldCategory() {
            IsFolded = true;
            _foldingSequence.Kill();
            _foldingSequence = DOTween.Sequence().SetUpdate(true);
            foreach (IModel child in Elements<ProficiencyEntryUI>()) {
                _foldingSequence.Join(child.View<IFoldingViewUI>().Unfold());
            }
        }
    }
}