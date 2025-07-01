using System;
using Awaken.TG.Editor.DataViews.Structure;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Filters {
    [Serializable]
    public abstract class DataViewFilter {
        public abstract bool Match(DataViewValue value);

        public abstract float DrawHeight();
        public abstract void Draw(Rect rect, ref bool changed);
    }
}