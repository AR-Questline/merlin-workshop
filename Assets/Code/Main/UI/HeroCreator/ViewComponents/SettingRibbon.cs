using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.HeroCreator.ViewComponents {
    public class SettingRibbon : ViewComponent {
        [BoxGroup("Main Elements"), Required] public TextMeshProUGUI nameText;
        [BoxGroup("Main Elements"), Required] public RectTransform container;
        [BoxGroup("Side Arrows")] public ARButton leftArrowButton, rightArrowButton;
        [BoxGroup("Side Arrows")] public RectTransform content;

        public void SetName(string name) => nameText.text = name;
    }
}
