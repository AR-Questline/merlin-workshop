using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class TapShortcut : Element<GameUI>, IUIHandlerSource, IUIAware {
        public override bool IsNotSaved => true;
        ControlSchemeFlag _controlSchemeFlag;
        readonly KeyBindings _keyBinding;
        readonly Action _callback;
        
        public UIContext Context => UIContext.All;
        public int Priority => 0;
        public bool IsValid => this.IsValidForUIHandle();

        public TapShortcut(KeyBindings keyBinding, Action callback, ControlSchemeFlag controlSchemeFlag = ControlSchemeFlag.All) {
            _keyBinding = keyBinding;
            _callback = callback;
            _controlSchemeFlag = controlSchemeFlag;
        }

        public UIResult Handle(UIEvent action) {
            if (!this.IsValidController(_controlSchemeFlag)) {
                return UIResult.Ignore;
            }
            
            if (action is UIKeyDownAction keyDownAction && keyDownAction.Name == _keyBinding.EnumName) {
                _callback?.Invoke();
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        public void ProvideHandlers(UIPosition position, List<IUIAware> handlers) {
            handlers.Add(this);
        }
    }
}