using Awaken.TG.Assets;
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
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public class VCPadShortcut : ViewComponent, INaviBlocker {
        [SerializeField] ShortcutPresence presence;

        [Space(5f)] 
        [SerializeField] bool customVisualOnly;
        [SerializeField, ShowIf(nameof(customVisualOnly))] ControllerKey.CustomVisualOnlyKey customVisualKey;
        [RichEnumExtends(typeof(KeyBindings.UI), typeof(KeyBindings.Gamepad)), RichEnumSearchBox]
        [SerializeField, HideIf(nameof(customVisualOnly))] RichEnumReference keyBinding;
        [SerializeField] bool hold;
        [SerializeField] bool autoSetupPadIcon;
        [SerializeField] bool autoSetupKeyboardIcon;

        [Space(5f)]
        [SerializeField] GameObject padIconHost;
        [SerializeField] GameObject keyboardIconHost;
        
        [Space(5f)]
        [SerializeField] Image padIcon;
        [SerializeField] Image additionalPadIcon;
        [SerializeField] Image padIconHoldBar;
        [SerializeField] Image keyboardIcon;

        public bool Hold => hold;
        
        ButtonShortcut _buttonShortcut;
        bool _active;

        public IShortcutAction Action { get; set; }
        public KeyBindings KeyBinding => keyBinding.EnumAs<KeyBindings>();
        public bool Active {
            get => _active && gameObject.activeInHierarchy && (Action?.Active ?? true);
            set {
                _active = value;
                Refresh();
            }
        }

        public bool AllowNavigation => presence == ShortcutPresence.Never;

        // === Initialize

        protected override void OnAttach() {
            Action = GetComponent<IShortcutAction>();
            if (Action != null) {
                Action.OnActiveChange += Refresh;
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
            
            if (Action != null) {
                _buttonShortcut = new ButtonShortcut(this);
                World.Only<GameUI>().AddElement(_buttonShortcut);
                
                _buttonShortcut.ListenTo(Events.HoldProceeded, OnHoldProceed, _buttonShortcut);
                _buttonShortcut.ListenTo(Events.HoldFinished, OnHoldFinish, _buttonShortcut);
            }

            TryAutoSetup(autoSetupKeyboardIcon, keyboardIcon, null, ControlScheme.KeyboardAndMouse);
            TryAutoSetup(autoSetupPadIcon, padIcon, additionalPadIcon, ControlScheme.Gamepad);
        }

        void TryAutoSetup(bool autoSetup, Image controllerIcon, Image additionalIcon, ControlScheme scheme) {
            var keyMapping = Services.Get<UIKeyMapping>();
            if (autoSetup && controllerIcon != null) {
                SpriteIcon iconSearch;
                if (customVisualOnly) {
                    iconSearch = keyMapping.GetCustomIconOf(customVisualKey, hold, scheme) as SpriteIcon;
                } else {
                    iconSearch = keyMapping.GetIconsOf(KeyBinding, hold)[scheme] as SpriteIcon;
                }
                
                TrySetSpriteIcon(iconSearch?.Sprite, controllerIcon);
                TrySetSpriteIcon(iconSearch?.AdditionalImage, additionalIcon);
            }
        }
        
        void TrySetSpriteIcon(SpriteReference spriteRef, Image image) {
            if (image == null) {
                return;
            }
            
            if (spriteRef is {IsSet: true}) {
                ParentView.RegisterReleasableHandle(spriteRef);
                spriteRef.SetSprite(image);
            }
        }

        // === Callbacks

        public void Refresh() {
            RefreshVisibility(RewiredHelper.IsGamepad ? ControllerType.Joystick : ControllerType.Keyboard);
        }
        void RefreshVisibility(ControllerType controllerType) {
            if (this == null) return;
            
            bool padActive = controllerType == ControllerType.Joystick && Active;
            if (padIconHost != null) {
                padIconHost.SetActive(padActive);
            } else {
                padIcon?.gameObject.SetActive(padActive);
                additionalPadIcon?.gameObject.SetActive(padActive);
            }
            
            bool keyboardActive = controllerType == ControllerType.Keyboard && Active;
            if (keyboardIconHost != null) {
                keyboardIconHost.SetActive(keyboardActive);
            } else {
                keyboardIcon?.gameObject.SetActive(keyboardActive);
            }
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
            if (Action != null) {
                Action.OnActiveChange -= Refresh;
            }
            _buttonShortcut?.Discard();
        }

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
