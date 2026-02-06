using System;
using System.Diagnostics;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Selections;
using Awaken.Utility.Collections;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.TG.MVC.UI.Handlers.States {
    /// <summary>
    /// Handles the global UI state, which influences many aspects of the UI - how hud is displayed,
    /// what is clickable, whether the map is scrollable, etc.
    /// </summary>
    public partial class UIStateStack : Model {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        public static UIStateStack Instance { get; private set; }

        // === Events
        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<UIStateStack, UIState> UIStateChanged = new(nameof(UIStateChanged));
        }

        // === Cache
        UIState _determinantState = UIState.BaseState;
        StructList<UIState> _removedStatesCache = new StructList<UIState>(1);

        // === State
        StructList<UIState> _orderedStates = new StructList<UIState>(12);

        public UIState State { get; private set; }

        // === Initialization
        protected override void OnInitialize() {
            Instance = this;
            var shortcutLayer = UIState.NewShortcutLayer;
            shortcutLayer.AssignOwner(this);
            _orderedStates.Add(shortcutLayer);
            Init();
        }

        void Init() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelInitialized<IUIStateSource>(), this, OnStateSourceAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelInitialized<IShortcut>(), this, AddShortcut);
            World.EventSystem.ListenTo(EventSelector.AnySource, Selection.Events.SelectionChanged, this, ForceRefresh);
            DetermineState();
            this.Trigger(Events.UIStateChanged, State);
        }

        // === UI State operations
        public void PushState(UIState state, IModel owner) {
            state.AssignOwner(owner);

            // This is not ideal if owner owns multiple states, but should be better than calling changes multiple times.
            owner.ListenTo(Model.Events.BeforeDiscarded, ReleaseAllOwnedBy, this);
            _orderedStates.Add(state);
            if (DetermineState()) {
                this.Trigger(Events.UIStateChanged, State);
            }
        }

        public void RemoveState(UIState state) {
            var stateChanged = false;
            for (int i = _orderedStates.Count - 1; i >= 0; i--) {
                if (ReferenceEquals(_orderedStates[i], state)) {
                    _orderedStates.RemoveAt(i);
                    stateChanged = DetermineState();
                    if (state.IsShortcutLayer) {
                        State.ShortcutLayer.AppendShortcuts(state);
                    }

                    var selection = World.Any<Selection>();
                    if (selection) {
                        selection.ClearSelectionLayer(state.SelectionLayer);
                    }
                    break;
                }
            }

            if (stateChanged) {
                this.Trigger(Events.UIStateChanged, State);
            }
        }

        public void ReleaseAllOwnedBy(IModel owner) {
            for (int i = _orderedStates.Count - 1; i >= 0; i--) {
                UIState uiState = _orderedStates[i];
                if (ReferenceEquals(uiState.Owner.Get(), owner)) {
                    _removedStatesCache.Add(uiState);
                    _orderedStates.RemoveAt(i);
                }
            }

            if (_removedStatesCache.Count == 0) {
                return;
            }

            var stateChanged = DetermineState();

            // We want to save all shortcuts from removed states to the newly active shortcuts, so that the only way for removal of a shortcut is by the person who registered them.
            foreach (var uiState in _removedStatesCache) {
                if (uiState.IsShortcutLayer) {
                    State.ShortcutLayer.AppendShortcuts(uiState);
                }
            }

            var selection = World.Any<Selection>();
            if (selection) {
                foreach (var state in _removedStatesCache) {
                    selection.ClearSelectionLayer(state.SelectionLayer);
                }
            }

            _removedStatesCache.Clear();

            if (stateChanged) {
                this.Trigger(Events.UIStateChanged, State);
            }
        }
        
        // === Callbacks
        void OnStateSourceAdded(Model model) {
            IUIStateSource stateSource = (IUIStateSource) model;
            VerifyStateSource(stateSource);
            PushState(stateSource.UIState, model);
        }

        public void ForceRefresh() {
            if (DetermineState()) {
                this.Trigger(Events.UIStateChanged, State);
            }
        }

        bool DetermineState() {
            var selected = World.Any<Selection>()?.Selected;
            foreach (var uiState in _orderedStates) {
                if (uiState.OnlyWhenSelected.id == null || ReferenceEquals(uiState.OnlyWhenSelected.Get(), selected)) {
                    uiState.RefreshShortcuts();
                    _determinantState.Union(uiState);
                }
            }

            if (_determinantState.Equals(State)) {
                if (State.ShortcutLayer == _determinantState.ShortcutLayer) {
                    _determinantState.ResetToBase();
                } else {
                    State = _determinantState;
                    _determinantState = UIState.BaseState;
                }
                return false;
            } else {
                State = _determinantState;
                _determinantState = UIState.BaseState;
                return true;
            }
        }

        [Conditional("DEBUG")]
        void VerifyStateSource(IUIStateSource stateSource) {
            bool NotSavedAttribute(IModel m) => m.IsNotSaved;
            bool MarkedNotSaved(IModel m) => m.MarkedNotSaved;
            bool NotSavedParents() {
                IModel current = (stateSource as IElement)?.GenericParentModel;
                while (current != null && !NotSavedAttribute(current) && !MarkedNotSaved(current)) {
                    current = (current as IElement)?.GenericParentModel;
                }

                return current != null;
            }

            if (!NotSavedAttribute(stateSource) && !MarkedNotSaved(stateSource) && !NotSavedParents()) {
                string msg = "Implementing IUIStateSource on models that are serialized is forbidden, because " +
                             $"UIStateStack doesn't implement serialization." +
                             $"\nModel: {stateSource.ID}({stateSource.GetType().FullName})";
                throw new InvalidOperationException(msg);
            }
        }
        
        // === Shortcuts
        void AddShortcut(Model model) {
            State.ShortcutLayer.AddShortcut((IShortcut)model);
        }

        protected override void OnFullyDiscarded() {
            Instance = null;
        }
    }
}