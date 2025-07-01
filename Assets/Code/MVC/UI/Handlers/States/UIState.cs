using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.MVC.UI.Handlers.States {
    /// <summary>
    /// Data object representing state of UI derived from model implementing <see cref="IUIStateSource"/>.
    /// Final UIState is determined by merging all UIStates bottom-to-top.
    /// Everything that has state related to UI model's stack should be implemented here.
    /// </summary>
    public partial class UIState : IEquatable<UIState> {
        public ushort TypeForSerialization => SavedTypes.UIState;

        public static int nextSelectionId = 1;

        // === Fields
        
        [Saved] List<WeakModelRef<IShortcut>> _shortcuts;

        [Saved] public WeakModelRef<IModel> Owner { get; private set; }
        [Saved] public HUDState HudState { get; private set; }
        [Saved] public int SelectionLayer { get; private set; }
        [Saved] public bool? MapInteractive { get; private set; }
        [Saved] public bool? ForceShowHeroBars { get; private set; }
        [Saved] public WeakModelRef<IModel> OnlyWhenSelected { get; private set; }
        [Saved] public bool PauseTime { get; private set; }
        [Saved] public bool PauseWeatherTime { get; private set; }
        [Saved] public UIState ShortcutLayer { get; private set; }
        [Saved] public bool HideCursor { get; private set; }

        public bool IsShortcutLayer {
            get => _shortcuts != null;
            private set {
                if (value) {
                    _shortcuts ??= new List<WeakModelRef<IShortcut>>();
                } else {
                    _shortcuts = null;
                }
            }
        }

        public bool IsMapInteractive => MapInteractive.GetValueOrDefault();

        // === Static creators

        public static UIState BaseState => new UIState(HUDState.None, 0, true);
        public static UIState TransparentState => new UIState(HUDState.None, -1, null);
        [UnityEngine.Scripting.Preserve] public static UIState Hidden => new UIState(HUDState.MiddlePanelShown, -1, true);
        public static UIState BlockInput => new UIState(HUDState.None, -1, false).WithCursorHidden();
        public static UIState Cursor => new UIState(HUDState.None, -1, false);
        public static UIState ModalState(HUDState hudState) => new UIState(hudState, nextSelectionId++, false);
        public static UIState NewShortcutLayer => new UIState(HUDState.None, -1, null).WithShortcutLayer();

        // === Constructors

        UIState() {}

        UIState(HUDState hudState, int selectionLayer, bool? mapInteractive) {
            this.HudState = hudState;
            this.SelectionLayer = selectionLayer;
            this.MapInteractive = mapInteractive;
        }

        // === Operations
        [UnityEngine.Scripting.Preserve]
        public UIState WhenSelected(IModel selected) {
            OnlyWhenSelected = new WeakModelRef<IModel>(selected);
            return this;
        }

        public UIState WithHUDState(HUDState hudState) {
            HudState = hudState;
            return this;
        }

        public UIState WithHeroBars(bool value) {
            ForceShowHeroBars = value;
            return this;
        }

        public UIState WithPauseTime() {
            PauseTime = true;
            return this;
        }
        
        public UIState WithPauseWeatherTime() {
            PauseWeatherTime = true;
            return this;
        }

        public UIState WithShortcutLayer() {
            IsShortcutLayer = true;
            return this;
        }
        public UIState WithCursorHidden() {
            HideCursor = true;
            return this;
        }
        
        public void AssignOwner(IModel owner) {
            Owner = new WeakModelRef<IModel>(owner);
        }

        /// <summary>
        /// All logic of merging UIStates lies here
        /// </summary>
        public UIState Merge(UIState other) {
            UIState newState = new UIState();
            newState.HudState = other.HudState | HudState;
            newState.SelectionLayer = Math.Max(SelectionLayer, other.SelectionLayer);
            newState.MapInteractive = other.MapInteractive ?? MapInteractive;
            newState.ForceShowHeroBars = other.ForceShowHeroBars ?? ForceShowHeroBars;
            newState.PauseTime = PauseTime || other.PauseTime;
            newState.PauseWeatherTime = PauseWeatherTime || other.PauseWeatherTime;
            newState.ShortcutLayer = other.IsShortcutLayer ? other : ShortcutLayer;
            newState.HideCursor = HideCursor || other.HideCursor;
            return newState;
        }

        public void AddShortcut(IShortcut shortcut) {
            _shortcuts.Add(new WeakModelRef<IShortcut>(shortcut));
        }
        public void AppendShortcuts(IEnumerable<WeakModelRef<IShortcut>> shortcuts) {
            _shortcuts.AddRange(shortcuts);
        }

        public bool ContainsShortcut(IShortcut shortcut) {
            if (_shortcuts == null) {
                return false;
            }
            var targetID = shortcut.ID;
            int count = _shortcuts.Count;
            for (int i = 0; i < count; i++) {
                if (_shortcuts[i].ID == targetID) {
                    return true;
                }
            }

            return false;
        }

        public void RefreshShortcuts() {
            _shortcuts?.RemoveAll(reference => reference.Get() == null);
        }

        public List<WeakModelRef<IShortcut>> GetShortcuts() {
            return _shortcuts;
        }

        // === Equality members
        
        public bool Equals(UIState other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return HudState == other.HudState 
                   && SelectionLayer == other.SelectionLayer
                   && MapInteractive == other.MapInteractive
                   && PauseTime == other.PauseTime
                   && PauseWeatherTime == other.PauseWeatherTime;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UIState) obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (int)HudState;
                hashCode = (hashCode * 397) ^ SelectionLayer;
                hashCode = (hashCode * 397) ^ MapInteractive.GetHashCode();
                hashCode = (hashCode * 397) ^ PauseTime.GetHashCode();
                hashCode = (hashCode * 397) ^ PauseWeatherTime.GetHashCode();
                return hashCode;
            }
        }
    }
}