using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Selections;
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
            public static readonly Event<UIStateStack, UIState> UIStatePopped = new(nameof(UIStatePopped));
            public static readonly Event<UIStateStack, UIState> UIStatePushed = new(nameof(UIStatePushed));
        }

        // === State
        List<UIState> _stateStack = new List<UIState>();
        public UIState State { get; private set; }

        // === Initialization
        protected override void OnInitialize() {
            Instance = this;
            _stateStack.Add(UIState.NewShortcutLayer);
            Init();
        }

        void Init() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelInitialized<IUIStateSource>(), this, OnStateSourceAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelInitialized<IShortcut>(), this, AddShortcut);
            World.EventSystem.ListenTo(EventSelector.AnySource, Model.Events.BeforeDiscarded, this, ReleaseAllOwnedBy);
            World.EventSystem.ListenTo(EventSelector.AnySource, Selection.Events.SelectionChanged, this, ForceRefresh);
            DetermineState();
            this.Trigger(Events.UIStateChanged, State);
        }

        // === UI State operations
        public void PushState(UIState state, IModel owner) {
            state.AssignOwner(owner);
            ModifyStateCollection(() => {
                _stateStack.Add(state);
            });
            this.Trigger(Events.UIStatePushed, state);
        }

        public void RemoveState(UIState state) {
            ModifyStateCollection(() => {
                for (int i = 0; i < _stateStack.Count; i++) {
                    if (!ReferenceEquals(_stateStack[i], state)) {
                        continue;
                    }
                
                    _stateStack.RemoveAt(i);
                    DetermineState();
                    if (state.IsShortcutLayer) {
                        State.ShortcutLayer.AppendShortcuts(state.GetShortcuts());
                    }
                    this.Trigger(Events.UIStatePopped, state);
                    break;
                }
            });
        }

        public void ReleaseAllOwnedBy(IModel owner) {
            List<UIState> toRemove = _stateStack.Where(s => ReferenceEquals(s.Owner.Get(), owner)).ToList();
            if (toRemove.Count == 0) return;

            ModifyStateCollection(() => RemoveAllFromOwner(owner));
            foreach (var state in toRemove) {
                this.Trigger(Events.UIStatePopped, state);
            }
        }

        void RemoveAllFromOwner(IModel owner) {
            // We want to save all shortcuts from removed states to the newly active shortcuts, so that the only way for removal of a shortcut is by the person who registered them.
            var cache = _stateStack.Where(sm => ReferenceEquals(sm.Owner.Get(), owner)).ToList();
            _stateStack.RemoveAll(sm => ReferenceEquals(sm.Owner.Get(), owner));
            DetermineState();
            foreach (UIState uiState in cache.Where(x => x.IsShortcutLayer)) {
                State.ShortcutLayer.AppendShortcuts(uiState.GetShortcuts());
            }
        }
        
        // === Callbacks
        void OnStateSourceAdded(Model model) {
            IUIStateSource stateSource = (IUIStateSource) model;
            VerifyStateSource(stateSource);
            PushState(stateSource.UIState, model);
        }

        public void ForceRefresh() {
            UIState previous = State;
            DetermineState();
            if (!previous.Equals(State)) {
                this.Trigger(Events.UIStateChanged, State);
            }
        }

        void ModifyStateCollection(Action action) {
            UIState previous = State;
            action();
            DetermineState();
            if (!previous.Equals(State)) {
                this.Trigger(Events.UIStateChanged, State);
            }
        }

        void DetermineState() {
            IModel selected = World.Any<Selection>()?.Selected;
            State = _stateStack
                .Where(state => state.OnlyWhenSelected.id == null || ReferenceEquals(state.OnlyWhenSelected.Get(), selected))
                .Aggregate(UIState.BaseState, (s1, s2) => {
                    s2.RefreshShortcuts();
                    return s1.Merge(s2);
                });
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