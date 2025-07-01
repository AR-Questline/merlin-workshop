using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.HeroCreator.ViewComponents {
    public class TextSelectSettingRibbon : SelectSettingRibbon<string> {
        public TextMeshProUGUI selectedText;

        protected override void OnAttach() {
            leftArrowButton.OnClick += IndexDecrement;
            rightArrowButton.OnClick += IndexIncrement;
        }

        protected override void OnChangeValue(int index, string value) {
            selectedText.text = value;
        }

        public override void SetOptions(string[] options, bool tryKeepIndex = false) {
            base.SetOptions(options, tryKeepIndex);
            var showArrows = options.Length > 1;
            leftArrowButton.gameObject.SetActive(showArrows);
            rightArrowButton.gameObject.SetActive(showArrows);
        }
    }
}
