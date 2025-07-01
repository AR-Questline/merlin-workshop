using System.Collections.Generic;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.MVC.UI.Handlers.Selections {
    /// <summary>
    /// Handles selection and deselection automatically for eligible views.
    /// </summary>
    public partial class Selection : Element<GameUI> {
        public sealed override bool IsNotSaved => true;

        readonly Dictionary<int, IModel> _selectedByLayer = new();
        readonly Dictionary<int, IEventListener> _listenersByLayer = new();
        
        // === Events
        public new static class Events {
            public static readonly Event<IModel, SelectionChange> SelectionChanged = new(nameof(SelectionChanged));
        }

        // === Selection management
        public int ActiveLayer { get; private set; }

        public bool IsSelected(IModel model) => ReferenceEquals(Selected, model);
        public IModel Selected {
            get => SelectedIn(ActiveLayer);
            private set => _selectedByLayer[ActiveLayer] = value;
        }

        IEventListener Listener {
            get => ListenerIn(ActiveLayer);
            set => _listenersByLayer[ActiveLayer] = value;
        }

        IModel SelectedIn(int layer) {
            _selectedByLayer.TryGetValue(layer, out var model);
            return model;
        }

        IEventListener ListenerIn(int layer) {
            _listenersByLayer.TryGetValue(layer, out var listener);
            return listener;
        }

        // === Initialization
        protected override void OnInitialize() {
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChange, this);
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStatePopped, OnUIStatePopped, this);
        }

        // === Select / Deselect
        
        /// <summary>
        /// Changes selection to a new one, triggering events.
        /// </summary>
        public void Select(IModel selectable) {
            if (ReferenceEquals(selectable, Selected)) return;
            // change selection
            var oldSelected = Selected;
            var oldListener = Listener;
            World.EventSystem.TryDisposeListener(ref oldListener);

            Selected = selectable;
            // trigger events about (de)selection
            oldSelected?.Trigger(Events.SelectionChanged, new SelectionChange(oldSelected, false));
            selectable?.Trigger(Events.SelectionChanged, new SelectionChange(selectable, true));
            // unselect model on discard
            Listener = selectable?.ListenTo(Model.Events.BeforeDiscarded, _ => Deselect(selectable), this);
        }

        /// <summary>
        /// Deselects a specified object. If this object is not currently selected,
        /// nothing happens.
        /// </summary>
        public void Deselect(IModel selectable) {
            if (ReferenceEquals(Selected, selectable)) Select(null);
        }

        /// <summary>
        /// Removes any currently active selection.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public void DeselectAll() {
            Select(null);
        }

        // === Callbacks
        void OnUIStateChange(UIState state) {
            if (state.SelectionLayer != ActiveLayer) {
                var oldSelected = Selected;
                ActiveLayer = state.SelectionLayer;
                oldSelected?.Trigger(Events.SelectionChanged, new SelectionChange(oldSelected, false));
                
                // check if new Selected wasn't discarded while waiting
                if (Selected?.WasDiscarded ?? false) {
                    Select(null);
                } else {
                    Selected?.Trigger(Events.SelectionChanged, new SelectionChange(Selected, true));
                }
            }
        }

        void OnUIStatePopped(UIState state) {
            _selectedByLayer.Remove(state.SelectionLayer);
            if (_listenersByLayer.Remove(state.SelectionLayer, out var listener)) {
                World.EventSystem.TryDisposeListener(ref listener);
            }
        }
        
        
        // === UI event handling
        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) {
            if (handler is ISelectableView selectable) {
                IModel responsibleModel = selectable.GenericTarget;
                if (evt is UIEMouseDown down && down.IsLeft) {
                    // automatically select on left click
                    Select(responsibleModel);
                    return UIResult.Accept;
                }
            }
            return UIResult.Ignore;
        }
    }
}
