using System;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Utils;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Types {
    public sealed class DataViewTypeEnum<TEnum> : DataViewType<TEnum> where TEnum : Enum {
        [UsedImplicitly] public static readonly DataViewTypeEnum<TEnum> Instance = new();

        readonly string[] _names = Enum.GetNames(typeof(TEnum));

        DataViewTypeEnum() {
            DataViewValue.GenericMethods.Cache<TEnum>.creator = Creator;
            DataViewValue.GenericMethods.Cache<TEnum>.getter = Getter;
            DataViewValue.GenericMethods.Cache<TEnum>.setter = Setter;
        }
        
        public override void Draw(in Rect rect, in DataViewValue value, out DataViewValue result, UniversalPtr metadata, out bool modified) {
            result = EditorGUI.Popup(rect, value.intValue, _names, DataViewStyle.Enum);
            modified = result.intValue != value.intValue;
        }

        public override int Compare(DataViewSorterPopup.Direction direction, in DataViewValue lhs, in DataViewValue rhs) {
            return DataViewSorterPopup.Compare(direction, lhs.intValue, rhs.intValue);
        }

        public override string ToString(DataViewValue value) {
            return _names[value.intValue];
        }

        public override bool TryParse(in ReadOnlySpan<char> value, ref DataViewValue result) {
            for (int i = 0; i < _names.Length; i++) {
                if (value.Equals(_names[i].AsSpan(), StringComparison.InvariantCultureIgnoreCase)) {
                    result.intValue = i;
                    return true;
                }
            }
            return false;
        }

        public override DataViewValue GetValue(SerializedProperty property) {
            return property.intValue;
        }

        public override void SetValue(SerializedProperty property, in DataViewValue value) {
            property.intValue = value.intValue;
        }

        public override DataViewFilter GetFilter() => new DataViewEnumFilter<TEnum>();
        
        static DataViewValue Creator(TEnum value) => value.ToInt();
        static TEnum Getter(in DataViewValue value) => value.intValue.ToEnum<TEnum>();
        static void Setter(ref DataViewValue dataViewValue, TEnum value) => dataViewValue.intValue = value.ToInt();
    }
}