using Awaken.TG.Main.UI.GamepadKeyboard.Keys;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.GamepadKeyboard {
    [UsesPrefab("UI/GamepadKeyboard/" + nameof(VKeyboard))]
    public class VKeyboard : View<Keyboard>, IAutoFocusBase, IFocusSource, IUIAware {
        
        // === Fields & Properties
        public bool ForceFocus => true;
        public Component DefaultFocus => defaultButton;
        
        [Header("Essentials")] 
        public Button defaultButton;
        public Transform keys;
        public Image capsLock, shift;

        TMP_InputField InputText => Target.InputField;
        string Input {
            get => InputText.text;
            set => InputText.text = value;
        }

        Key[] _keyList;
        bool _capslockFlag, _shiftFlag;
        
        protected override void OnInitialize() {
            _keyList = keys.GetComponentsInChildren<Key>();
            foreach (var key in _keyList) {
                key.OnKeyClicked += GenerateInput;
            }

            _shiftFlag = !string.IsNullOrWhiteSpace(Input);
            Shift();
        }

        public void Backspace() {
            if (Input.Length > 0) {
                Input = Input.Remove(Input.Length - 1);
                if (Input.Length <= 0) {
                    _shiftFlag = false;
                    Shift();
                }
            }
        }

        public void Clear() {
            Input = "";
            _shiftFlag = false;
            Shift();
        }

        public void CapsLock() {
            _capslockFlag = !_capslockFlag;
            
            foreach (var key in _keyList) {
                if (key is Alphabet) {
                    key.CapsLock(_capslockFlag);
                }
            }
            
            RefreshButtons();
        }

        public void Shift() {
            _shiftFlag = !_shiftFlag;
        
            foreach (var key in _keyList) {
                if (key is Alphabet) {
                    key.ShiftKey(_shiftFlag);
                }
            }
            
            RefreshButtons();
        }

        public void Enter() {
            if (RewiredHelper.IsGamepad) {
                Target.Trigger(Keyboard.Events.InputAccepted, true);
            }
            if (!Target.HasBeenDiscarded) {
                Target.Discard();
            }
        }

        public void GenerateInput(string s) {
            Input += s;
            _shiftFlag = false;
            foreach (var key in _keyList) {
                if (key is Alphabet) {
                    key.ShiftKey(_shiftFlag);
                }
            }

            RefreshButtons();
        }

        void RefreshButtons() {
            capsLock.color = _capslockFlag ? ARColor.SpecialAccent : new Color(0.3921569f, 0.3921569f, 0.3921569f);
            shift.color = _shiftFlag ? ARColor.SpecialAccent :  new Color(0.3921569f, 0.3921569f, 0.3921569f);
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UICancelAction) {
                if (RewiredHelper.IsGamepad) {
                    Target.Trigger(Keyboard.Events.InputCanceled, true);
                }
                if (Target is { HasBeenDiscarded: false }) {
                    Target.Discard();
                }
                return UIResult.Accept;
            }
            return UIResult.Ignore;
        }
    }
}