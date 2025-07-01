using System;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.Utility.LowLevel;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Types {
    public sealed class DataViewTypeBool : DataViewType<bool> {
        [UsedImplicitly] public static readonly DataViewTypeBool Instance = new();

        DataViewTypeBool() {
            DataViewValue.GenericMethods.Cache<bool>.creator = Creator;
            DataViewValue.GenericMethods.Cache<bool>.getter = Getter;
            DataViewValue.GenericMethods.Cache<bool>.setter = Setter;
        }
        
        public override void Draw(in Rect rect, in DataViewValue value, out DataViewValue result, UniversalPtr metadata, out bool modified) {
            result = EditorGUI.Toggle(rect, value.boolValue);
            modified = result.boolValue != value.boolValue;
        }

        public override int Compare(DataViewSorterPopup.Direction direction, in DataViewValue lhs, in DataViewValue rhs) {
            return DataViewSorterPopup.Compare(direction, lhs.boolValue, rhs.boolValue);
        }

        public override string ToString(DataViewValue value) {
            return value.boolValue ? "True" : "False";
        }

        public override bool TryParse(in ReadOnlySpan<char> value, ref DataViewValue result) {
            return bool.TryParse(value, out result.boolValue);
        }

        public override DataViewValue GetValue(SerializedProperty property) {
            return property.boolValue;
        }

        public override void SetValue(SerializedProperty property, in DataViewValue value) {
            property.boolValue = value.boolValue;
        }

        public override DataViewFilter GetFilter() => new DataViewBoolFilter();
        
        static DataViewValue Creator(bool value) => value;
        static bool Getter(in DataViewValue value) => value.boolValue;
        static void Setter(ref DataViewValue dataViewValue, bool value) => dataViewValue.boolValue = value;
    }
}