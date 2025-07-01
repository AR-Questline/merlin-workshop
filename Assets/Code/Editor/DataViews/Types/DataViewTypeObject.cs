using System;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.Utility.LowLevel;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Types {
    public sealed class DataViewTypeObject<TObject> : DataViewType<TObject> where TObject : Object {
        [UsedImplicitly] public static readonly DataViewTypeObject<TObject> Instance = new();

        DataViewTypeObject() {
            DataViewValue.GenericMethods.Cache<TObject>.creator = Creator;
            DataViewValue.GenericMethods.Cache<TObject>.getter = Getter;
            DataViewValue.GenericMethods.Cache<TObject>.setter = Setter;
        }
        
        public override void Draw(in Rect rect, in DataViewValue value, out DataViewValue result, UniversalPtr metadata, out bool modified) {
            result = EditorGUI.ObjectField(rect, value.objectReferenceValue, typeof(TObject), false);
            modified = result.objectReferenceValue != value.objectReferenceValue;
        }

        public override int Compare(DataViewSorterPopup.Direction direction, in DataViewValue lhs, in DataViewValue rhs) {
            return DataViewSorterPopup.Compare(direction, lhs.objectReferenceValue?.name, rhs.objectReferenceValue?.name);
        }

        public override bool SupportExporting() => true;
        public override string ToString(DataViewValue value) => value.objectReferenceValue?.name;
        public override bool TryParse(in ReadOnlySpan<char> value, ref DataViewValue result) => false;

        public override DataViewValue GetValue(SerializedProperty property) {
            return property.objectReferenceValue;
        }

        public override void SetValue(SerializedProperty property, in DataViewValue value) {
            property.objectReferenceValue = value.objectReferenceValue;
        }

        public override DataViewFilter GetFilter() => new DataViewObjectFilter();
        
        static DataViewValue Creator(TObject value) => value;
        static TObject Getter(in DataViewValue value) => value.objectReferenceValue as TObject;
        static void Setter(ref DataViewValue dataViewValue, TObject value) => dataViewValue.objectReferenceValue = value;
    }
}