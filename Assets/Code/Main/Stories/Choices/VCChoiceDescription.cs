using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using Awaken.TG.Utility;
using TMPro;

namespace Awaken.TG.Main.Stories.Choices {
    /// <summary>
    /// Show description to choice button 
    /// </summary>
    public class VCChoiceDescription : ViewComponent<Story> {

        public TextMeshProUGUI description;
        public TextMeshProUGUI effectAndCost;

        public void Enable() {
            Clear();
            World.EventSystem.ListenTo(EventSelector.AnySource, Hovering.Events.HoverChanged, this, OnHoverChange);
        }

        protected override void OnAttach() {
            Enable();
        }

        void OnHoverChange(HoverChange hoverChange) {
            if (hoverChange.Hovered && hoverChange.View is VChoice choice) {
                effectAndCost.text = choice.EffectAndCost.FormatSprite();
            } else {
                Clear();
            }
        }

        void Clear() {
            description.text = string.Empty;
            effectAndCost.text = string.Empty;
        }
    }
}
