using System;
using Awaken.TG.Main.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.BalanceTool.UI.Controls {
    [UxmlElement]
    public partial class TemplateStatField<T> : VisualElement {
        public VisualElement content;
        public TextValueField<T> valueField;
        public Label lastValueLabel;
        public Button applyButton;
        
        EventCallback<ChangeEvent<T>> _changedCallback;
        Action _applyCallback;

        public TemplateStatField() { }

        public TemplateStatField(TextValueField<T> valueField) {
            content = new();
            content.style.flexDirection = FlexDirection.Row;
            this.Add(content);

            this.valueField = valueField;
            this.valueField.style.flexGrow = 1;
            
            lastValueLabel = new();
            lastValueLabel.style.fontSize = 10;
            lastValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            lastValueLabel.SetActiveOptimized(false);
            
            applyButton = new();
            applyButton.text = "Apply";
            applyButton.SetActiveOptimized(false);
            
            content.Add(this.valueField);
            content.Add(applyButton);
            content.Add(lastValueLabel);
        }
        
        public void SetApplyControlsState(bool state, T lastValue) {
            applyButton.SetActiveOptimized(state);
            lastValueLabel.SetActiveOptimized(state);
            lastValueLabel.text = lastValue.ToString();
        }

        public void RegisterChangeCallback(EventCallback<ChangeEvent<T>> callback) {
            _changedCallback = callback;
            valueField.RegisterValueChangedCallback(_changedCallback);
        }
        
        public void RegisterApplyCallback(Action callback) {
            _applyCallback = callback;
            applyButton.clicked += _applyCallback;
        }

        public void Discard() {
            if (_changedCallback != null) {
                valueField.UnregisterValueChangedCallback(_changedCallback);
            }
            if (_applyCallback != null) {
                applyButton.clicked -= _applyCallback;
            }
        }
    }
}
