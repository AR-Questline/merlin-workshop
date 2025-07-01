using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class VCShowIfSelectedWithController : ViewComponent {
        public ARButton button;

        protected override void OnAttach() {
            if (button == null) {
                button = GetComponentInParent<ARButton>();
            }
            button.OnSelected += OnSelected;
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, Refresh);
            Refresh();
        }

        void OnSelected(bool selected) {
            Refresh();
        }

        void Refresh() {
            gameObject.SetActive(RewiredHelper.IsGamepad && button.Selected);
        }
    }
}