using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI {
    public class VCLocString : ViewComponent {
        [SerializeField, LocStringCategory(Category.UI)] LocString locString;
        [SerializeField] TextMeshProUGUI text;

        protected override void OnAttach() {
            text.SetText(locString);
        }

        void Reset() {
            TryGetComponent(out text);
        }
    }
}