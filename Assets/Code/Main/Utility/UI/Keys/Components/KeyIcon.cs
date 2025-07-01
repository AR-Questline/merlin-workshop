using Awaken.TG.Assets;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Keys.Components {
    public class KeyIcon : MonoBehaviour {
        protected Data _data;
        protected ControlScheme _currentScheme;
        protected SpriteReference _loadedIconRef;
        protected SpriteReference _loadedAdditionalImageRef;
        protected SpriteReference _loadedHoldRef;
        
        ByControlScheme<IIconSearchResult> _icons;
        [CanBeNull] UIKeyMapping _internalKeyMapping;
        readonly IEventListener[] _refreshListeners = new IEventListener[2];
        
        protected string KeyBindingLog => _currentScheme == ControlScheme.Gamepad 
            ? $"Scheme: {_currentScheme} Gamepad: {_data.GamepadKey.EnumName} Hold: {_data.IsHold}" 
            : $"Scheme: {_currentScheme} Key: {_data.KeyboardKey.EnumName} Hold: {_data.IsHold}";
        
        public virtual void Setup(in Data data, IListenerOwner listenerOwner) {
            _data = data;
            
            var focus = World.Only<Focus>();
            _refreshListeners[0] = focus.ListenTo(Focus.Events.KeyMappingRefreshed, RefreshIcons, listenerOwner);
            _refreshListeners[1] = focus.ListenTo(Focus.Events.ControllerChanged, RefreshIcon, listenerOwner);
            
            RefreshIcons();
        }
        
        public virtual void SetupWithoutMVC(in Data data, UIKeyMapping keyMapping) {
            _internalKeyMapping = keyMapping;
            _data = data;
            RefreshIcons();
        }

        public virtual void SetHoldPercent(float value) { }
        protected virtual void SetupTextIcon(TextIcon textIcon) { }
        protected virtual void SetupSpriteIcon(SpriteIcon spriteIcon) { }
        protected virtual void OnIconNull() { }
        protected virtual void OnUnknownIconType() { }
        protected virtual void OnPostIconSetup() { }
        
        public void RefreshIcons() {
            _icons = _data.GetIcons(_internalKeyMapping);
            RefreshIcon();
        }
        
        void RefreshIcon() {
            if (_data.HasCorrectSetup == false) {
                Log.Minor?.Warning("Key binding is missing for KeyIcon component. Possibly incorrect setup, please check", gameObject);
                OnIconNull();
                return;
            }

            if (Configuration.GetBool("ui.disable-prompts")) {
                OnIconNull();
                return;
            }
            
            _currentScheme = ControlSchemes.Current();
            var currentIcon = _icons[_currentScheme];
            
            if (currentIcon is SpriteIcon spriteIcon) {
                SetupSpriteIcon(spriteIcon);
            } else if (currentIcon is TextIcon textIcon) {
                SetupTextIcon(textIcon);
            } else if (currentIcon == null) {
                Log.Important?.Error($"No icon set for key binding: {KeyBindingLog}", gameObject);
                OnIconNull();
            } else {
                Log.Important?.Error($"Unknown KeyIcon for key binding: {KeyBindingLog}", gameObject);
                OnUnknownIconType();
            }

            OnPostIconSetup();
        }
        
        void OnDestroy() {
            var eventSystem = World.Services.Get<EventSystem>();
            for (int i = 0; i < _refreshListeners.Length; i++) {
                eventSystem.TryDisposeListener(ref _refreshListeners[i]);
            }
            
            _loadedIconRef?.Release();
            _loadedHoldRef?.Release();
            _loadedAdditionalImageRef?.Release();
        }

        public struct Data {
            public KeyBindings KeyboardKey { get; private set; }
            public KeyBindings GamepadKey { get; private set; }
            public readonly bool IsHold => _keyboardAndMouseHold || _gamepadHold;
            public readonly bool HasCorrectSetup => KeyboardKey != null && GamepadKey != null;
            
            ControllerKey.Mouse MouseOverride { get; set; }

            bool _keyboardAndMouseHold;
            bool _gamepadHold;
            bool _keyboardOverriden;
            bool _gamepadOverriden;
            bool _mouseOverriden;

            public Data(KeyBindings key, bool hold) : this() {
                KeyboardKey = key;
                GamepadKey = key;
                _keyboardAndMouseHold = hold;
                _gamepadHold = hold;
            }

            [UnityEngine.Scripting.Preserve]
            public Data OverrideKeyboard(KeyBindings key) => OverrideKeyboard(key, _keyboardAndMouseHold);
            public Data OverrideKeyboard(KeyBindings key, bool hold) {
                _mouseOverriden = false;
                _keyboardOverriden = true;
                KeyboardKey = key;
                _keyboardAndMouseHold = hold;
                return this;
            }

            public Data OverrideGamepad(KeyBindings key) => OverrideGamepad(key, _gamepadHold);
            public Data OverrideGamepad(KeyBindings key, bool hold) {
                _gamepadOverriden = true;
                GamepadKey = key;
                _gamepadHold = hold;
                return this;
            }

            public Data OverrideMouse(ControllerKey.Mouse icon) => OverrideMouse(icon, _keyboardAndMouseHold);
            public Data OverrideMouse(ControllerKey.Mouse icon, bool hold) {
                _keyboardOverriden = false;
                _mouseOverriden = true;
                MouseOverride = icon;
                _keyboardAndMouseHold = hold;
                return this;
            }
            
            public readonly ByControlScheme<IIconSearchResult> GetIcons(UIKeyMapping internalKeyMapping = null) {
                var keyMapping = internalKeyMapping != null ? internalKeyMapping : World.Services.Get<UIKeyMapping>();
                var icons = keyMapping.GetIconsOf(KeyboardKey, _keyboardAndMouseHold);

                if (_keyboardOverriden) {
                    icons[ControlScheme.KeyboardAndMouse] = keyMapping.GetIconOf(KeyboardKey, _keyboardAndMouseHold, ControlScheme.KeyboardAndMouse);
                }

                if (_gamepadOverriden) {
                    icons[ControlScheme.Gamepad] = keyMapping.GetIconOf(GamepadKey, _gamepadHold, ControlScheme.Gamepad);
                }

                if (_mouseOverriden) {
                    icons[ControlScheme.KeyboardAndMouse] = keyMapping.GetIconOf(MouseOverride, _keyboardAndMouseHold);
                }

                return icons;
            }
        }
    }
}