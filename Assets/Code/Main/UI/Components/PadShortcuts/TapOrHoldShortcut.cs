using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public partial class TapOrHoldShortcut : Element<GameUI>, IUIHandlerSource, IUIAware, IShortcut, IButtonTap, IButtonHold {
        public sealed override bool IsNotSaved => true;

        ButtonsHandler _handler = new();

        VCTapOrHoldShortcut _shortCut;
        VCTapOrHoldShortcut Shortcut {
            get {
                if (_shortCut != null) return _shortCut;
                Discard();
                return null;
            }
        }

        bool Active => (Shortcut?.Active ?? false) && this.IsActive();
        
        IShortcutAction TapAction => Shortcut.tapAction;
        IShortcutAction HoldAction => Shortcut.holdAction;
        KeyBindings PadKeyBinding => Shortcut.padKeyBinding.EnumAs<KeyBindings>();
        KeyBindings TapKeyboardKeyBinding => Shortcut.keyboardTapKeyBinding.EnumAs<KeyBindings>();
        KeyBindings HoldKeyboardKeyBinding => Shortcut.keyboardHoldKeyBinding.EnumAs<KeyBindings>();
        
        public UIContext Context => UIContext.Keyboard;
        public int Priority => 0;

        public TapOrHoldShortcut(VCTapOrHoldShortcut shortCut) {
            _shortCut = shortCut;
        }

        public void ProvideHandlers(UIPosition position, List<IUIAware> handlers) {
            handlers.Add(this);
        }

        public UIResult Handle(UIEvent evt) {
            if (!Active) return UIResult.Ignore;
            return evt switch {
                UIKeyAction action when action.Name == PadKeyBinding => _handler.HandleTapAndHold(this, this, action),
                UIKeyDownAction keyDown when keyDown.Name == TapKeyboardKeyBinding => TapAction.Invoke(),
                UIKeyDownAction keyDown when keyDown.Name == HoldKeyboardKeyBinding => HoldAction.Invoke(),
                _ => UIResult.Ignore
            };
        }

        void IButtonTap.Invoke() => TapAction.Invoke();
        void IButtonTap.OnTap() { }
        void IButtonHold.OnKeyDown() { }
        void IButtonHold.OnKeyHeld(float percent) => this.Trigger(PadShortcuts.Events.HoldProceeded, percent);
        void IButtonHold.OnKeyUp(bool completed) => this.Trigger(PadShortcuts.Events.HoldFinished, this);
        void IButtonHold.Invoke() => HoldAction.Invoke();
    }
}