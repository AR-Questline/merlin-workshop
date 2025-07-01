using System;
using Awaken.TG.Main.UIToolkit.Utils;
using Awaken.Utility.Editor.UTK.Utils;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Tags {
    public static class TagsTableFactory {
        public const string ColumnName = "name";
        public const string ColumnContext = "context";
        public const string ColumnActions = "actions";
        
        public static void SetupColumn(TagsSection section, string columnName, Func<VisualElement> makeCell) {
            section.Table.SetupColumn(columnName, makeCell, BindCell);
            return;

            void BindCell(VisualElement element, int i) {
                SetupElement(columnName, element, i, section, () => section.RemoveTag(i));
            }
        }

        static void SetupElement(string columnName, VisualElement element, int index, TagsSection section, Action callback = null) {
            TagValueAccessor accessor = section.Table.GetItemDataForIndex<TagValueAccessor>(index);
            element.dataSource = accessor;
            
            switch (columnName) {
                case ColumnName:
                    Label label = (Label) element;
                    label.text = accessor.Token;
                    BindNoninteractiveElement(new PropertyPath(nameof(TagValueAccessor.Token)), element);
                    break;
                case ColumnContext:
                    TextField textField = (TextField) element;
                    textField.value = accessor.Context;
                    BindNoninteractiveElement(new PropertyPath(nameof(TagValueAccessor.Context)), element);
                    break;
                case ColumnActions:
                    Button button = (Button) element;
                    button.text = "Remove";
                    button.clickable = new Clickable(callback);
                    break;
                default:
                    throw MultiColumnViewUtils.UnknownColumn(columnName);
            }
        }

        static void BindNoninteractiveElement(PropertyPath propertyPath, VisualElement element) {
            var binding = DataBindingUtils.CreateDefaultBinding(propertyPath, element);
            element.SetBinding(binding.target, binding.dataBinding);
        }
    }
}
