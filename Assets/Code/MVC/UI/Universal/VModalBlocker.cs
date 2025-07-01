using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Animations;
using UnityEngine;
using IEvent = Awaken.TG.MVC.UI.Events.IEvent;

namespace Awaken.TG.MVC.UI.Universal {
    /// <summary>
    /// A canvas view that blocks all UI events going lower into the stack.
    /// When clicked, it sends a special event to the model, so that it
    /// (or its view) can hide the modal.
    /// </summary>
    [UsesPrefab("UI/VModalBlocker")]
    public class VModalBlocker : View<Model>, IUIAware, IAutoFocusBase, IFocusSource {
        UIState _uiState;
        
        // === Auto Focus
        public bool ForceFocus => true;
        public Component DefaultFocus => this;

        // === Events
        public static class Events {
            public static readonly Event<IModel, IEvent> ModalDismissed = new(nameof(ModalDismissed));
        }

        // === Initialization
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            _uiState = UIState.NewShortcutLayer;
            UIStateStack.Instance.PushState(_uiState, Target);
        }

        // === UI implementation
        public virtual UIResult Handle(UIEvent evt) {
            // on click, let the model or its view handle it
            if (evt is ISubmit action && Target is { HasBeenDiscarded: false }) {
                Target.Trigger(Events.ModalDismissed, action);
                return UIResult.Accept;
            } else if (evt is UIKeyAction) {
                return UIResult.Ignore;
            }

            // either way, block anything from going further
            return UIResult.Prevent;
        }

        protected override IBackgroundTask OnDiscard() {
            UIStateStack.Instance.RemoveState(_uiState);
            return base.OnDiscard();
        }
    }
}