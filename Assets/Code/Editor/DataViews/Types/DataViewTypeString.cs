using System;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Utils;
using Awaken.Utility.LowLevel;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Types {
    public sealed class DataViewTypeString : DataViewType<string> {
        [UsedImplicitly] public static readonly DataViewTypeString Instance = new();

        static readonly GUIContent ReusableGUIContent = new();
        
        static GUIStyle Style => DataViewStyle.Text;
        
        DataViewTypeString() {
            DataViewValue.GenericMethods.Cache<string>.creator = Creator;
            DataViewValue.GenericMethods.Cache<string>.getter = Getter;
            DataViewValue.GenericMethods.Cache<string>.setter = Setter;
        }

        public override UniversalPtr CreateMetadata() {
            return UniversalPtr.CreateUnmanaged<Vector2>();
        }

        public override void FreeMetadata(ref UniversalPtr ptr) {
            ptr.FreeUnmanaged();
        }

        public override void Draw(in Rect rect, in DataViewValue value, out DataViewValue result, UniversalPtr metadata, out bool modified) {
            ReusableGUIContent.text = value.stringValue;
            var height = Style.CalcHeight(ReusableGUIContent, rect.width);
            if (height <= rect.height) {
                result = EditorGUI.TextField(rect, value.stringValue, Style);
            } else {
                var width = rect.width - 15f;
                height = Style.CalcHeight(ReusableGUIContent, width);
                var viewRect = new Rect {
                    width = width,
                    height = height
                };
                ref var scroll = ref metadata.GetUnmanaged<Vector2>();
                scroll = GUI.BeginScrollView(rect, scroll, viewRect, false, false);
                result = EditorGUI.TextField(viewRect, value.stringValue, Style);
                GUI.EndScrollView();
            }
            modified = result.stringValue != value.stringValue;
        }

        public override int Compare(DataViewSorterPopup.Direction direction, in DataViewValue lhs, in DataViewValue rhs) {
            return DataViewSorterPopup.Compare(direction, lhs.stringValue, rhs.stringValue);
        }

        public override string ToString(DataViewValue value) {
            return value.stringValue.Replace('\n', '|');
        }

        public override bool TryParse(in ReadOnlySpan<char> value, ref DataViewValue result) {
            result.stringValue = value.ToString().Replace('|', '\n');
            return true;
        }

        public override DataViewValue GetValue(SerializedProperty property) {
            return property.stringValue;
        }

        public override void SetValue(SerializedProperty property, in DataViewValue value) {
            property.stringValue = value.stringValue;
        }

        public override DataViewFilter GetFilter() => new DataViewStringFilter();
        
        static DataViewValue Creator(string value) => value;
        static string Getter(in DataViewValue value) => value.stringValue;
        static void Setter(ref DataViewValue dataViewValue, string value) => dataViewValue.stringValue = value;
    }
}