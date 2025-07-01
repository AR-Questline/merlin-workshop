using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public class VCCPartHost : View<CharacterCreatorPart> {
        [SerializeField] TextMeshProUGUI title;

        protected override void OnInitialize() {
            title.SetActiveAndText(!string.IsNullOrEmpty(Target.Title), Target.Title);
        }
    }
}