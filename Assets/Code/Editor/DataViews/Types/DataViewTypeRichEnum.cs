using System;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Utils;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Awaken.Utility.LowLevel;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Types {
    public sealed class DataViewTypeRichEnum<TRichEnum> : DataViewType<TRichEnum> where TRichEnum : RichEnum {
        [UsedImplicitly] public static readonly DataViewTypeRichEnum<TRichEnum> Instance = new();

        public TRichEnum[] enums;
        public string[] names;
        public string[] refs;

        static DataViewTypeRichEnum() {
            DataViewTypeRichEnum<StatType>.Instance.SetEnums(RichEnumCache.GetDerived<StatType>());
            DataViewTypeRichEnum<KeyBindings>.Instance.SetEnums(RichEnumCache.GetDerived<KeyBindings>());
        }
        
        DataViewTypeRichEnum() {
            SetEnums(RichEnum.AllValuesOfType<TRichEnum>());
            
            DataViewValue.GenericMethods.Cache<TRichEnum>.creator = Creator;
            DataViewValue.GenericMethods.Cache<TRichEnum>.getter = Getter;
            DataViewValue.GenericMethods.Cache<TRichEnum>.setter = Setter;
        }

        void SetEnums(TRichEnum[] enums) {
            this.enums = enums;
            names = new string[enums.Length];
            refs = new string[enums.Length];
            for (int i = 0; i < enums.Length; i++) {
                names[i] = enums[i].EnumName;
                refs[i] = RichEnumReference.GetEnumRef(enums[i]);
            }
        }
        
        public override void Draw(in Rect rect, in DataViewValue value, out DataViewValue result, UniversalPtr metadata, out bool modified) {
            result = EditorGUI.Popup(rect, value.intValue, names, DataViewStyle.Enum);
            modified = result.intValue != value.intValue;
        }

        public override int Compare(DataViewSorterPopup.Direction direction, in DataViewValue lhs, in DataViewValue rhs) {
            return DataViewSorterPopup.Compare(direction, lhs.intValue, rhs.intValue);
        }

        public override string ToString(DataViewValue value) {
            return value.intValue < 0 ? "" : names[value.intValue];
        }

        public override bool TryParse(in ReadOnlySpan<char> value, ref DataViewValue result) {
            for (int i = 0; i < names.Length; i++) {
                if (value.Equals(names[i].AsSpan(), StringComparison.InvariantCultureIgnoreCase)) {
                    result.intValue = i;
                    return true;
                }
            }
            return false;
        }

        public override DataViewValue GetValue(SerializedProperty property) {
            return refs.IndexOf(EnumRefProperty(property).stringValue);
        }

        public override void SetValue(SerializedProperty property, in DataViewValue value) {
            EnumRefProperty(property).stringValue = refs[value.intValue];
        }

        public override DataViewFilter GetFilter() => new DataViewRichEnumFilter<TRichEnum>();

        static SerializedProperty EnumRefProperty(SerializedProperty property) {
            return property.FindPropertyRelative("_enumRef");
        }
        
        static DataViewValue Creator(TRichEnum value) => Instance.enums.IndexOf(value);
        static TRichEnum Getter(in DataViewValue value) => Instance.enums[value.intValue];
        static void Setter(ref DataViewValue dataViewValue, TRichEnum value) => dataViewValue.intValue = Instance.enums.IndexOf(value);
    }
}