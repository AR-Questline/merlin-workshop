namespace Awaken.TG.Main.UI.GamepadKeyboard.Keys {
    public class Alphabet : Key {
        bool _isCapslockEnabled, _isShiftEnabled;
        
        public override void CapsLock(bool isUppercase) {
            _isCapslockEnabled = isUppercase;
            Refresh();
        }

        public override void ShiftKey(bool isPressed) {
            _isShiftEnabled = isPressed;
            Refresh();
        }

        void Refresh() {
            _key.text = (_isCapslockEnabled || _isShiftEnabled) ? _key.text.ToUpper() : _key.text.ToLower();
        }
    }
}