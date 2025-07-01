using Awaken.TG.MVC;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI {
    public class VCLocTermString : ViewComponent {
        [SerializeField] string locTermId;
        [SerializeField] TextMeshProUGUI text;

        protected override void OnAttach() {
            text.SetText(locTermId.Translate());
        }

        void Reset() {
            TryGetComponent(out text);
        }
    }
}