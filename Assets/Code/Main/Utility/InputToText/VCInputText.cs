using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using TMPro;

namespace Awaken.TG.Main.Utility.InputToText {
    public class VCInputText : ViewComponent {
        [Required]
        public TMP_Text textComponent;
        [LocStringCategory(Category.UI)]
        public LocString initialText;

        protected override void OnAttach() {
            textComponent.text = Services.Get<InputToTextMapping>().ReplaceInText(initialText);
        }

        public void SetText(string text) {
            textComponent.text = Services.Get<InputToTextMapping>().ReplaceInText(text);
        }

        void OnValidate() {
            if (textComponent == null) {
                textComponent = GetComponent<TMP_Text>();
            }
        }
    }
}