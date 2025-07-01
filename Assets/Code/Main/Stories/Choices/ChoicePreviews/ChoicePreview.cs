using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Stories.Choices.ChoicePreviews {
    [SpawnsView(typeof(VChoicePreview))]
    public partial class ChoicePreview : Element<Choice> {
        public sealed override bool IsNotSaved => true;

        public readonly string choicePreviewTitle;
        readonly StructList<IHoverInfo> _proficiencyInfos;

        public ChoicePreview(in StructList<IHoverInfo> proficiencyInfos) {
            this._proficiencyInfos = proficiencyInfos;
            choicePreviewTitle = _proficiencyInfos.FirstOrDefault()?.InfoGroupName;
        }

        protected override void OnInitialize() {
            foreach (var profInfo in _proficiencyInfos) {
                AddElement(new BonusPreview(profInfo));
            }
        }
    }
}