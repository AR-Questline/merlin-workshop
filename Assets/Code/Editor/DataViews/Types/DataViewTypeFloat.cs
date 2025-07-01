using System;
using System.Globalization;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Utils;
using Awaken.Utility.LowLevel;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Types {
    public sealed class DataViewTypeFloat : DataViewType<float> {
        [UsedImplicitly] public static readonly DataViewTypeFloat Instance = new();

        DataViewTypeFloat() {
            DataViewValue.GenericMethods.Cache<float>.creator = Creator;
            DataViewValue.GenericMethods.Cache<float>.getter = Getter;
            DataViewValue.GenericMethods.Cache<float>.setter = Setter;
        }
        
        public override void Draw(in Rect rect, in DataViewValue value, out DataViewValue result, UniversalPtr metadata, out bool modified) {
            result = EditorGUI.FloatField(rect, value.floatValue, DataViewStyle.Number);
            modified = math.abs(result.floatValue - value.floatValue) > float.Epsilon;
        }

        public override int Compare(DataViewSorterPopup.Direction direction, in DataViewValue lhs, in DataViewValue rhs) {
            return DataViewSorterPopup.Compare(direction, lhs.floatValue, rhs.floatValue);
        }

        public override string ToString(DataViewValue value) {
            return value.floatValue.ToString(CultureInfo.CurrentCulture);
        }

        public override bool TryParse(in ReadOnlySpan<char> value, ref DataViewValue result) {
            return float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result.floatValue);
        }

        public override DataViewValue GetValue(SerializedProperty property) {
            return property.floatValue;
        }

        public override void SetValue(SerializedProperty property, in DataViewValue value) {
            property.floatValue = value.floatValue;
        }

        public override DataViewFilter GetFilter() => new DataViewFloatFilter();
        
        static DataViewValue Creator(float value) => value;
        static float Getter(in DataViewValue value) => value.floatValue;
        static void Setter(ref DataViewValue dataViewValue, float value) => dataViewValue.floatValue = value;
    }
}