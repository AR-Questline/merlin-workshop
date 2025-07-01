using System;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.Utils {
    public static class DataBindingUtils {
        public static (string target, DataBinding dataBinding) CreateDefaultBinding(string propertyName, VisualElement element) {
            return (GetDefaultBingingTarget(element), CreateDefaultBinding(propertyName));
        }
        
        public static (string target, DataBinding dataBinding) CreateDefaultBinding(PropertyPath propertyPath, VisualElement element) {
            return (GetDefaultBingingTarget(element), CreateDefaultBinding(propertyPath));
        }
        
        [UnityEngine.Scripting.Preserve]
        public static (string target, DataBinding dataBinding) CreateDefaultTargetBinding(string propertyName, VisualElement element, BindingMode bindingMode = BindingMode.TwoWay) {
            DataBinding binding = new() {
                dataSourcePath = new PropertyPath(propertyName),
                bindingMode = bindingMode
            };
            
            return (GetDefaultBingingTarget(element), binding);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static (string target, DataBinding dataBinding) CreateDefaultTargetBinding(PropertyPath propertyPath, VisualElement element, BindingMode bindingMode = BindingMode.TwoWay) {
            DataBinding binding = new() {
                dataSourcePath = propertyPath,
                bindingMode = bindingMode
            };
            
            return (GetDefaultBingingTarget(element), binding);
        }
        
        static DataBinding CreateDefaultBinding(string propertyName) {
            return new DataBinding {
                dataSourcePath = new PropertyPath(propertyName)
            };
        }
        
        static DataBinding CreateDefaultBinding(PropertyPath propertyPath) {
            return new DataBinding {
                dataSourcePath = propertyPath
            };
        }
            
        static string GetDefaultBingingTarget(VisualElement element) {
            return element switch {
                Label => nameof(Label.text),
                IntegerField => nameof(IntegerField.value),
                FloatField => nameof(FloatField.value),
                TextField => nameof(TextField.value),
                _ => throw new ArgumentOutOfRangeException(nameof(element), element.GetType().Name, "Unknown element type")
            };
        }
    }
}