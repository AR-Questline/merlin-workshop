using System;
using System.Collections.Generic;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class HoldWithFillShortcut : Element<GameUI>, IUIHandlerSource, IUIAware {
        public override bool IsNotSaved => true;

        ControlSchemeFlag _controlSchemeFlag;
        readonly KeyBindings _keyBinding;
        readonly Image _fillImage;
        readonly float _buttonHoldDuration;
        readonly Action _callback;
        
        float _holdStartTime;
        bool _heldButton;
        
        public bool IsValid => this.IsValidForUIHandle();
        public UIContext Context => UIContext.All;
        public int Priority => 0;

        public HoldWithFillShortcut(KeyBindings keyBinding, Image fillImage, float buttonHoldDuration, Action callback, ControlSchemeFlag controlSchemeFlag = ControlSchemeFlag.All) {
            _keyBinding = keyBinding;
            _fillImage = fillImage;
            _buttonHoldDuration = buttonHoldDuration;
            _callback = callback;
            _controlSchemeFlag = controlSchemeFlag;
        }

        public UIResult Handle(UIEvent action) {
            if (!this.IsValidController(_controlSchemeFlag)) {
                return UIResult.Ignore;
            }

            if (!_heldButton && action is UIKeyDownAction keyDownAction && keyDownAction.Name == _keyBinding.EnumName) {
                _heldButton = true;
                _holdStartTime = Time.unscaledTime;
                return UIResult.Accept;
            }
        
            if (_heldButton && action is UIKeyHeldAction keyHeldAction && keyHeldAction.Name == _keyBinding.EnumName) {
                float holdTime = Time.unscaledTime - _holdStartTime;
                if (holdTime <= _buttonHoldDuration) {
                    _fillImage.fillAmount = holdTime / _buttonHoldDuration;
                } else {
                    _heldButton = false;
                    _fillImage.fillAmount = 0f;
                    _callback?.Invoke();
                }
                return UIResult.Accept;
            }
        
            if (_heldButton && action is UIKeyUpAction keyUpAction && keyUpAction.Name == _keyBinding.EnumName) {
                float holdTime = Time.unscaledTime - _holdStartTime;
                if (holdTime < _buttonHoldDuration) {
                    _fillImage.fillAmount = 0f;
                }
                _heldButton = false;
                return UIResult.Accept;
            }
        
            return UIResult.Ignore;
        }
        
        public void ProvideHandlers(UIPosition _, List<IUIAware> handlers) {
            handlers.Add(this);
        }
    }
}