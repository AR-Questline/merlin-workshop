using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class VCTextColorFromButton : ViewComponent {
        public ARButton button;
        public TextMeshProUGUI text;
        public bool customColors = false;
        [ShowIf(nameof(customColors)),SerializeField] Color normalColor = Color.white;
        [ShowIf(nameof(customColors)),SerializeField] Color hoverColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1);
        [ShowIf(nameof(customColors)),SerializeField] Color selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1);
        [ShowIf(nameof(customColors)),SerializeField] Color pressColor = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1);
        [ShowIf(nameof(customColors)),SerializeField] Color disableColor = new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.5019608f);

        protected override void OnAttach() {
            if (customColors) {
                text.color = normalColor;
                button.OnHover += hovered => text.color = hovered ? hoverColor : normalColor;
                button.OnPress += () => text.color = pressColor;
                button.OnRelease += () => text.color = button.NormalColor;
                button.OnSelected += selected => text.color = selected ? selectedColor : normalColor;
                button.OnInteractableChange += interactable => text.color = interactable ? normalColor : disableColor;
            } else {
                text.color = button.NormalColor;
                button.OnHover += hovered => text.color = hovered ? button.HoverColor : button.NormalColor;
                button.OnPress += () => text.color = button.PressColor;
                button.OnRelease += () => text.color = button.NormalColor;
                button.OnSelected += selected => text.color = selected ? button.SelectedColor : button.NormalColor;
                button.OnInteractableChange += interactable => text.color = interactable ? button.NormalColor : button.DisableColor;
            }
        }
    }
}
