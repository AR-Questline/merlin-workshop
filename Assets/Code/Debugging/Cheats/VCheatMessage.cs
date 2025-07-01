using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Debugging.Cheats {
    [UsesPrefab("VCheatMessage")]
    public class VCheatMessage : View<CheatController> {
        [LocStringCategory(Category.UI)]
        public LocString message;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            GetComponent<TMP_Text>().text = message.ToString();
        }

        void LateUpdate() {
            transform.SetAsLastSibling();
        }
    }
}