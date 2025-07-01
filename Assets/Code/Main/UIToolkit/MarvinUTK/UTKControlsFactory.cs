using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.MarvinUTK {
    public abstract class UTKControlsFactory<T> : IUTKControlsFactory<T> where T : VisualElement, new() {
        protected readonly List<string> customUssClasses = new();
        readonly string _name;

        protected UTKControlsFactory (string name, params string[] customUssClasses) {
            _name = name;
            this.customUssClasses.AddRange(customUssClasses);
        }

        public virtual T Create() {
            var element = new T();
            SetupElement(element);
            return element;
        }
        
        protected void SetupElement(T element) {
            if (string.IsNullOrEmpty(_name) == false) {
                element.name = _name;
            }
            
            customUssClasses.ForEach(element.AddToClassList);
        }
    }
    
    public class UTKLabelFactory : UTKControlsFactory<Label> {
        string Text { get; set; } = string.Empty;
        
        [UnityEngine.Scripting.Preserve]
        public UTKLabelFactory(string name = null, params string[] customUssClasses) : base(name, customUssClasses) { }
        public UTKLabelFactory(string text, string name = null, params string[] customUssClasses) : base(name, customUssClasses) { 
            Text = text;
        }

        public override Label Create() {
            Label element = base.Create();
            element.text = Text;
            return element;
        }
    }
    
    public class UTKElementFactory : UTKControlsFactory<VisualElement> {
        VisualElement Element { get; set; }
        
        public UTKElementFactory(VisualElement element, string name = null, params string[] customUssClasses) : base(name, customUssClasses) { 
            Element = element;
        }

        public override VisualElement Create() {
            customUssClasses.ForEach(Element.AddToClassList);
            return Element;
        }
    }
    
    public class UTKButtonFactory : UTKControlsFactory<Button> {
        string Text { get; set; } = string.Empty;
        [UnityEngine.Scripting.Preserve] Clickable ClickCallback { get; set; }

        [UnityEngine.Scripting.Preserve]
        public UTKButtonFactory(string name = null, params string[] customUssClasses) : base(name, customUssClasses) { }
        public UTKButtonFactory(string text, Clickable clickCallback, string name = null, params string[] customUssClasses) : base(name, customUssClasses) { 
            Text = text;
            ClickCallback = clickCallback;
        }

        public override Button Create() {
            Button element = base.Create();
            element.text = Text;
            element.clickable = ClickCallback;
            return element;
        }
    }
    
    public class UTKSearchFieldFactory : UTKControlsFactory<TextField> {
        string PlaceholderText { get; set; } = string.Empty;
        [UnityEngine.Scripting.Preserve] EventCallback<ChangeEvent<string>> SearchCallback { get; set; }

        [UnityEngine.Scripting.Preserve]
        public UTKSearchFieldFactory(string name = null, params string[] customUssClasses) : base(name, customUssClasses) { }
        public UTKSearchFieldFactory(string text, EventCallback<ChangeEvent<string>> searchCallback, string name = null, params string[] customUssClasses) : base(name, customUssClasses) { 
            PlaceholderText = text;
            SearchCallback = searchCallback;
        }

        public override TextField Create() {
            TextField element = base.Create();
            element.SetValueWithoutNotify(PlaceholderText);
            element.RegisterValueChangedCallback(changeEvent => {
                SearchCallback?.Invoke(changeEvent);
                TryFillPlaceholderText(changeEvent, element);
            });
            return element;
        }

        void TryFillPlaceholderText(ChangeEvent<string> evt, TextField textField) {
            if (textField.focusController.focusedElement != textField && string.IsNullOrWhiteSpace(evt.newValue)) {
                textField.SetValueWithoutNotify(PlaceholderText);
            }
        }
    }
}