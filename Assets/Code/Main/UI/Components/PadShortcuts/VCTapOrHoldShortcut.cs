using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Universal;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class VCTapOrHoldShortcut : ViewComponent, INaviBlocker {
        public ShortcutPresence presence;
        
        [Space(5f)]
        [RichEnumExtends(typeof(KeyBindings)), RichEnumSearchBox]
        public RichEnumReference padKeyBinding;
        [RichEnumExtends(typeof(KeyBindings)), RichEnumSearchBox]
        public RichEnumReference keyboardTapKeyBinding;
        [RichEnumExtends(typeof(KeyBindings)), RichEnumSearchBox]
        public RichEnumReference keyboardHoldKeyBinding;
        public bool autoSetupPadIcon;

        [Space(5f)]
        public Image padTapIcon;
        public Image padHoldIcon;
        public Image padIconHoldBar;
        public Image keyboardTapIcon;
        public Image keyboardHoldIcon;
        public ButtonShortcutAction tapAction, holdAction;
        
        TapOrHoldShortcut _buttonShortcut;
        bool _active;
        
        public bool Active {
            get => _active && gameObject.activeInHierarchy && ((tapAction?.Active ?? true) || (holdAction?.Active ?? true));
            set {
                _active = value;
                Refresh();
            }
        }

        public bool AllowNavigation => presence == ShortcutPresence.Never;

        // === Initialize

        protected override void OnAttach() {
            if (tapAction != null && holdAction != null) {
                tapAction.OnActiveChange += Refresh;
                holdAction.OnActiveChange += Refresh;
            }

            Active = presence switch {
                ShortcutPresence.Always => true,
                ShortcutPresence.WhenFocused => IsParentOf(World.Only<Focus>().Focused?.transform, transform),
                _ => false
            };
            
            if (padIconHoldBar != null) {
                padIconHoldBar.fillAmount = 0;
            }

            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.FocusChanged, this, OnFocusChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, RefreshVisibility);
            
            if (tapAction != null && holdAction != null) {
                _buttonShortcut = new TapOrHoldShortcut(this);
                World.Only<GameUI>().AddElement(_buttonShortcut);
                
                _buttonShortcut.ListenTo(Events.HoldProceeded, OnHoldProceed, _buttonShortcut);
                _buttonShortcut.ListenTo(Events.HoldFinished, OnHoldFinish, _buttonShortcut);
            }

            if (autoSetupPadIcon && padTapIcon != null && padHoldIcon != null) {
                var keyMap = Services.Get<UIKeyMapping>();
                var tapSpriteRef = ((keyMap.GetIconsOf(padKeyBinding.EnumAs<KeyBindings>(), false))[ControlScheme.Gamepad] as SpriteIcon)?.Sprite;
                var holdSpriteRef = ((keyMap.GetIconsOf(padKeyBinding.EnumAs<KeyBindings>(), true))[ControlScheme.Gamepad] as SpriteIcon)?.OverrideHoldAnimation;

                if (tapSpriteRef is {IsSet: true}) {
                    ParentView.RegisterReleasableHandle(tapSpriteRef);
                    tapSpriteRef.SetSprite(padTapIcon);
                }

                if (padIconHoldBar != null && holdSpriteRef is {IsSet: true}) {
                    ParentView.RegisterReleasableHandle(holdSpriteRef);
                    holdSpriteRef.SetSprite(padHoldIcon);
                }
            }
        }

        // === Callbacks
        public void Refresh() {
            RefreshVisibility(RewiredHelper.IsGamepad ? ControllerType.Joystick : ControllerType.Keyboard);
        }
        void RefreshVisibility(ControllerType controllerType) {
            if (this == null) return;
        
            padTapIcon?.gameObject.SetActive(controllerType == ControllerType.Joystick && Active);
            padHoldIcon?.gameObject.SetActive(controllerType == ControllerType.Joystick && Active);
            keyboardTapIcon?.gameObject.SetActive(controllerType == ControllerType.Keyboard && Active);
            keyboardHoldIcon?.gameObject.SetActive(controllerType == ControllerType.Keyboard && Active);
        }

        void OnFocusChanged(FocusChange focusChange) {
            if (this == null) return;
            
            if (presence == ShortcutPresence.WhenFocused) {
                Active = IsParentOf(focusChange.current?.transform, transform);
            }
        }

        void OnHoldProceed(float percent) {
            if (padIconHoldBar != null) {
                padIconHoldBar.fillAmount = percent;
            }
        }
        void OnHoldFinish() {
            if (padIconHoldBar != null) {
                padIconHoldBar.fillAmount = 0;
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (tapAction != null && holdAction != null) {
                tapAction.OnActiveChange -= Refresh;
                holdAction.OnActiveChange -= Refresh;
            }
            _buttonShortcut?.Discard();
        }
        
        // === Helpers
        static bool IsParentOf(Transform parent, Transform children) {
            if (parent == null) return false;
            while (true) {
                if (children.parent == null) return false;
                if (children.parent == parent) return true;
                children = children.parent;
            }
        }
    }
}