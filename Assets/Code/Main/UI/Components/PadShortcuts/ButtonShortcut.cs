using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public partial class ButtonShortcut : Element<GameUI>, IUIHandlerSource, IUIAware, IShortcut, IButtonTap, IButtonHold {
        public sealed override bool IsNotSaved => true;
        
        ButtonsHandler _handler = new();

        VCPadShortcut _padShortcut;
        VCPadShortcut PadShortcut {
            get {
                if (_padShortcut != null) return _padShortcut;
                Discard();
                return null;
            }
        }
        
        bool Active => (PadShortcut?.Active ?? false) && this.IsActive();
        IShortcutAction Action => PadShortcut.Action;
        KeyBindings KeyBinding => PadShortcut.KeyBinding;
        bool Hold => PadShortcut.Hold;
        
        public UIContext Context => UIContext.Keyboard;
        public int Priority => 0;

        public ButtonShortcut(VCPadShortcut padShortcut) {
            _padShortcut = padShortcut;
        }

        public void ProvideHandlers(UIPosition position, List<IUIAware> handlers) {
            handlers.Add(this);
        }

        public UIResult Handle(UIEvent evt) {
            if (Active && evt is UIKeyAction action && action.Name == KeyBinding) {
                return Hold ? _handler.HandleHold(this, action) : _handler.HandleTap(this, action);
            } else {
                return UIResult.Ignore;
            }
        }

        void IButtonTap.Invoke() => Action.Invoke();
        void IButtonTap.OnTap() { }
        void IButtonHold.OnKeyDown() { }
        void IButtonHold.OnKeyHeld(float percent) => this.Trigger(PadShortcuts.Events.HoldProceeded, percent);
        void IButtonHold.OnKeyUp(bool completed) => this.Trigger(PadShortcuts.Events.HoldFinished, this);
        void IButtonHold.Invoke() => Action.Invoke();
    }
    
    public static class Events {
        public static readonly Event<IShortcut, float> HoldProceeded = new(nameof(HoldProceeded));
        public static readonly Event<IShortcut, IShortcut> HoldFinished = new(nameof(HoldFinished));
    }
}