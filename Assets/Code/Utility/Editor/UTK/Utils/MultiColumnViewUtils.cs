using System;
using Awaken.TG.Main.UIToolkit;
using UnityEngine.UIElements;

namespace Awaken.Utility.Editor.UTK.Utils {
    public static class MultiColumnViewUtils {
        public static void SetupColumn<T>(this MultiColumnTreeView tree, string columnName, Action<VisualElement, int> bindCell, Action<VisualElement, int> unbindCell = null, string headerTooltip = null) where T : VisualElement, new() {
            SetupColumn<T>(tree.columns, columnName, bindCell, unbindCell, headerTooltip);
        }
        
        public static void SetupColumn(this MultiColumnTreeView tree, string columnName, Func<VisualElement> makeCell, Action<VisualElement, int> bindCell, Action<VisualElement, int> unbindCell = null, string headerTooltip = null) {
            SetupColumn(tree.columns, columnName, makeCell, bindCell, unbindCell, headerTooltip);
        }
        
        public static void SetupColumn<T>(this MultiColumnListView list, string columnName, Action<VisualElement, int> bindCell, Action<VisualElement, int> unbindCell = null, string headerTooltip = null) where T : VisualElement, new() {
            SetupColumn<T>(list.columns, columnName, bindCell, unbindCell, headerTooltip);
        }
        
        public static void SetupColumn(this MultiColumnListView list, string columnName, Func<VisualElement> makeCell, Action<VisualElement, int> bindCell, Action<VisualElement, int> unbindCell = null, string headerTooltip = null) {
            SetupColumn(list.columns, columnName, makeCell, bindCell, unbindCell, headerTooltip);
        }
        
        public static Func<VisualElement> GetDefaultMakeCell<T>() where T : VisualElement, new() {
            return () => new T();
        }

        static void SetupColumn<T>(Columns columns, string columnName, Action<VisualElement, int> bindCell, Action<VisualElement, int> unbindCell, string headerTooltip) where  T : VisualElement, new() {            
            SetupColumn(columns, columnName, GetDefaultMakeCell<T>(), bindCell, unbindCell, headerTooltip);
        }
        
        static void SetupColumn(Columns columns, string columnName, Func<VisualElement> makeCell,  Action<VisualElement, int> bindCell, Action<VisualElement, int> unbindCell, string headerTooltip) {            
            columns[columnName].makeCell = makeCell;
            columns[columnName].bindCell = bindCell;
            columns[columnName].unbindCell = unbindCell;
            
            if (!string.IsNullOrWhiteSpace(headerTooltip)) {
                columns[columnName].makeHeader = () => {
                    VisualElement header = new() {
                        pickingMode = PickingMode.Ignore
                    };
                    header.AddToClassList("unity-multi-column-header__column__default-content");
                    Label title = new();
                    title.AddToClassList("unity-multi-column-header__column__title");
                    title.SetActiveOptimized(true);
                    header.Add(title);
                    return header;
                };
                columns[columnName].bindHeader = element => {
                    Label field = element.Q<Label>();
                    field.text = columns[columnName].title;
                    field.tooltip = headerTooltip;
                };
            }
        }
        
        public static ArgumentOutOfRangeException UnknownColumn(string columnName) => new (nameof(columnName), columnName, "Unknown column name");
    }
}
