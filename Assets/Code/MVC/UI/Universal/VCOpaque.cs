using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.MVC.UI.Universal {
    /// <summary>
    /// Universal view components that will block any UI events from going
    /// "through" the component.
    /// </summary>
    public class VCOpaque : ViewComponent<Model>, IUIAware {
        public bool isNewShortcutLayer;

        protected override void OnAttach() {
            if (isNewShortcutLayer) {
                UIStateStack.Instance.PushState(UIState.NewShortcutLayer, Target);
            }
        }

        public UIResult Handle(UIEvent evt) {
            return UIResult.Prevent;
        } 
    }
}
