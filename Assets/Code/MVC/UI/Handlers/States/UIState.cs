using Awaken.Utility;
using System;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Collections;

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
        
        [Saved] StructList<WeakModelRef<IShortcut>> _shortcuts;

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
            get => _shortcuts.IsCreated;
            private set {
                if (value) {
                    if (!_shortcuts.IsCreated) {
                        _shortcuts = new StructList<WeakModelRef<IShortcut>>(1);
                    }
                } else {
                    _shortcuts.Uncreate();
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
        public void ResetToBase() {
            HudState = HUDState.None;
            SelectionLayer = 0;
            MapInteractive = true;

            _shortcuts.Uncreate();
            Owner = null;
            ForceShowHeroBars = null;
            OnlyWhenSelected = null;
            PauseTime = false;
            PauseWeatherTime = false;
            ShortcutLayer = null;
            HideCursor = false;
        }

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

        public void Union(UIState other) {
            HudState = other.HudState | HudState;
            SelectionLayer = Math.Max(SelectionLayer, other.SelectionLayer);
            MapInteractive = other.MapInteractive ?? MapInteractive;
            ForceShowHeroBars = other.ForceShowHeroBars ?? ForceShowHeroBars;
            PauseTime = PauseTime || other.PauseTime;
            PauseWeatherTime = PauseWeatherTime || other.PauseWeatherTime;
            ShortcutLayer = other.IsShortcutLayer ? other : ShortcutLayer;
            HideCursor = HideCursor || other.HideCursor;
        }

        public void AddShortcut(IShortcut shortcut) {
            _shortcuts.Add(new WeakModelRef<IShortcut>(shortcut));
        }

        public void AppendShortcuts(UIState other) {
            foreach (var shortcut in other._shortcuts) {
                if (shortcut.Get() != null) {
                    _shortcuts.Add(shortcut);
                }
            }
        }

        public bool ContainsShortcut(IShortcut shortcut) {
            if (!_shortcuts.IsCreated) {
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
            for (int i = _shortcuts.Count - 1; i >= 0; i--) {
                var shortcut = _shortcuts[i];
                if (shortcut.Get() == null) {
                    _shortcuts.RemoveAt(i);
                }
            }
        }

        // === Equality members
        
        public bool Equals(UIState other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return HudState == other.HudState 
                   && SelectionLayer == other.SelectionLayer
                   && MapInteractive == other.MapInteractive
                   && ForceShowHeroBars == other.ForceShowHeroBars
                   && PauseTime == other.PauseTime
                   && PauseWeatherTime == other.PauseWeatherTime
                   && HideCursor == other.HideCursor;
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