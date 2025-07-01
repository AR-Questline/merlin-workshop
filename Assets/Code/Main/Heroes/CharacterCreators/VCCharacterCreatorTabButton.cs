using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    public class VCCharacterCreatorTabButton : CharacterCreatorTabs.VCTabButton {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] GameObject selection;
        [Space(10f)]
        [RichEnumExtends(typeof(CharacterCreatorTabType))]
        [SerializeField] RichEnumReference type;
        public override CharacterCreatorTabType Type => type.EnumAs<CharacterCreatorTabType>();

        protected override void OnAttach() {
            base.OnAttach();
            buttonConfig.InitializeButton();
        }

        protected override void Refresh(bool selected) {
            selection.SetActive(selected);
        }
    }
}