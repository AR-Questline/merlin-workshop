using System;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    public partial class EditNameUI : InputValueUI<string> {
        public sealed override bool IsNotSaved => true;

        readonly string _initialValue;
        readonly Action _onKeyboardInputAccepted;
        readonly Action _onKeyboardInputCanceled;
        
        public bool InitialValueChanged => _initialValue != Value;
        public string ErrorMsg { get; private set; }
        public bool IsInputFieldFocused { get; set; }
        
        public new static class Events {
            public static readonly Event<EditNameUI, bool> InputFieldFocusChanged = new(nameof(InputFieldFocusChanged));
        }
        
        public EditNameUI(string initialValue = "", Action inputAcceptedCallback = null, Action inputCanceledCallback = null, bool isInputFieldFocused = false) {
            _initialValue = initialValue;
            Value = initialValue;
            ErrorMsg = string.Empty;
            _onKeyboardInputAccepted = inputAcceptedCallback;
            _onKeyboardInputCanceled = inputCanceledCallback;
            IsInputFieldFocused = isInputFieldFocused;
        }

        public override void ChangeValue(string newValue) {
            base.ChangeValue(newValue);
            Validate();
        }

        public bool Validate() {
            if (string.IsNullOrWhiteSpace(Value)) {
                ErrorMsg = LocTerms.SlotNameLengthTooShort.Translate();
                return false;
            }

            if (PlatformUtils.IsPlatformWithLanguageRestrictions() && ForbiddenTerms.terms.Any(t => Value.ToLower().Contains(t, StringComparison.InvariantCultureIgnoreCase))) {
                ErrorMsg = LocTerms.SlotNameForbidden.Translate();
                return false;
            }
            
            ErrorMsg = string.Empty;
            return true;
        }
        
        public void OnInputKeyboardAccepted() {
            if (Validate()) {
                _onKeyboardInputAccepted?.Invoke();
            }
        }
        
        public void OnInputKeyboardCanceled() {
            _onKeyboardInputCanceled?.Invoke();
        }
    }
}