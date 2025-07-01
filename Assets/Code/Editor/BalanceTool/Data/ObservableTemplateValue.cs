using System;
using System.Collections.Generic;

namespace Awaken.TG.Editor.BalanceTool.Data {
    public class ObservableTemplateValue<T> where T : struct {
        public Func<T> TemplateValue { get; }
        public event Action<(T previousValue, T newValue)> OnValueChanged = delegate { };
        
        T? _changedValue;
        Action<T> _applyCallback;
        
        public T CurrentValue {
            get => _changedValue ?? TemplateValue.Invoke();
            set {
                if (EqualityComparer<T>.Default.Equals(value, CurrentValue)) return;
                _changedValue = value;
                
                if (_changedValue != null) {
                    OnValueChanged.Invoke((CurrentValue, _changedValue.Value));
                }
            }
        }
            
        public bool IsChanged => _changedValue != null;

        public ObservableTemplateValue(Func<T> templateValueGetter, Action<T> applyCallback) {
            TemplateValue = templateValueGetter;
            _applyCallback = applyCallback;
        }

        public void Apply() {
            if (IsChanged) {
                _applyCallback?.Invoke(CurrentValue);
                ResetChange();
            }
        }
            
        void ResetChange() {
            _changedValue = null;
        }
    }
}
