using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews {
    [Serializable]
    public class DataViewFilterPopup {
        DataViewFilterList _filter = new();
        
        public void Draw(Rect rect, DataViewHeader[] headers, out bool changed) {
            changed = false;
            _filter.Draw(rect, headers, ref changed);
        }

        public DataViewRow[] Filter(DataViewRow[] originalRows, DataViewHeader[] headers) {
            return FilterEnumerable(originalRows, headers).ToArray();
        }
        
        IEnumerable<DataViewRow> FilterEnumerable(DataViewRow[] originalRows, DataViewHeader[] headers) {
            var filters = _filter.GetEntries(headers);
            foreach (var row in originalRows) {
                if (filters.Count == 0 || filters.All(filter => filter.Match(row))) {
                    yield return row;
                }
            }
        }

        public void Clear() {
            _filter.Clear();
        }
    }
}