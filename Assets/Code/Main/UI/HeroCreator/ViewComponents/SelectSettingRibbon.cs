using UnityEngine;

namespace Awaken.TG.Main.UI.HeroCreator.ViewComponents {
    public abstract class SelectSettingRibbon<T> : SettingRibbon {
        protected T[] _options;
        int _selectedIndex;

        public int SelectedIndex {
            get => _selectedIndex;
            set {
                _selectedIndex = Mathf.Clamp(value, 0,_options.Length);
                if (_options != null && _selectedIndex >= 0 && _selectedIndex < _options.Length) {
                    OnChangeValue(_selectedIndex, _options[_selectedIndex]);
                    onChange?.Invoke(_selectedIndex);
                }
            }
        }
        
        public delegate void OnChange(int index);

        public event OnChange onChange;
        
        protected abstract void OnChangeValue(int index, T value);

        public void IndexDecrement() {
            if (_options == null) return;
            if (_options.Length > 1) {
                SelectedIndex = (SelectedIndex - 1 + _options.Length) % _options.Length;
            } else if (SelectedIndex != 0) {
                SelectedIndex = 0;
            }
        }

        public void IndexIncrement() {
            if (_options == null) return;
            if (_options.Length > 1) {
                SelectedIndex = (SelectedIndex + 1) % _options.Length;
            } else if (SelectedIndex != 0) {
                SelectedIndex = 0;
            }
        }

        public virtual void SetOptions(T[] options, bool tryKeepIndex = false) {
            _options = options;
            if (tryKeepIndex && SelectedIndex < options.Length) {
                SelectedIndex = SelectedIndex;
            } else {
                SelectedIndex = 0;
            }
        }
    }
}
