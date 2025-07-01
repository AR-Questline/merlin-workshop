using System;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Utils;
using Awaken.Utility.LowLevel;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Types {
    public sealed class DataViewTypeInt : DataViewType<int> {
        [UsedImplicitly] public static readonly DataViewTypeInt Instance = new();

        DataViewTypeInt() {
            DataViewValue.GenericMethods.Cache<int>.creator = Creator;
            DataViewValue.GenericMethods.Cache<int>.getter = Getter;
            DataViewValue.GenericMethods.Cache<int>.setter = Setter;
        }
        
        public override void Draw(in Rect rect, in DataViewValue value, out DataViewValue result, UniversalPtr metadata, out bool modified) {
            result = EditorGUI.IntField(rect, value.intValue, DataViewStyle.Number);
            modified = result.intValue != value.intValue;
        }

        public override int Compare(DataViewSorterPopup.Direction direction, in DataViewValue lhs, in DataViewValue rhs) {
            return DataViewSorterPopup.Compare(direction, lhs.intValue, rhs.intValue);
        }

        public override string ToString(DataViewValue value) {
            return value.intValue.ToString();
        }

        public override bool TryParse(in ReadOnlySpan<char> value, ref DataViewValue result) {
            return int.TryParse(value, out result.intValue);
        }

        public override DataViewValue GetValue(SerializedProperty property) {
            return property.intValue;
        }

        public override void SetValue(SerializedProperty property, in DataViewValue value) {
            property.intValue = value.intValue;
        }

        public override DataViewFilter GetFilter() => new DataViewIntFilter();
        
        static DataViewValue Creator(int value) => value;
        static int Getter(in DataViewValue value) => value.intValue;
        static void Setter(ref DataViewValue dataViewValue, int value) => dataViewValue.intValue = value;
    }
}