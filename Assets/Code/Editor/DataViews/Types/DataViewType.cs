using System;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.Utility.LowLevel;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Types {
    public abstract class DataViewType {
        public abstract void Draw(in Rect rect, in DataViewValue value, out DataViewValue result, UniversalPtr metadata, out bool modified);
        public abstract int Compare(DataViewSorterPopup.Direction direction, in DataViewValue lhs, in DataViewValue rhs);
        public virtual bool SupportExporting() => true;
        public abstract string ToString(DataViewValue value);
        public abstract bool TryParse(in ReadOnlySpan<char> value, ref DataViewValue result);
        
        public abstract DataViewValue GetValue(SerializedProperty property);
        public abstract void SetValue(SerializedProperty property, in DataViewValue value);

        public abstract Filters.DataViewFilter GetFilter();

        public virtual UniversalPtr CreateMetadata() => default;
        public virtual void FreeMetadata(ref UniversalPtr ptr) { }
    }
    public abstract class DataViewType<T> : DataViewType { }
}