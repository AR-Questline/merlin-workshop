using Awaken.TG.Editor.DataViews.Headers;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.MoreGUI;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Utils {
    public static class DataViewDrawing {
        public static void Header(Rect rect, ref string header, DataViewHeader[] headers) {
            Header(rect, ref header, headers, out _, out _);
        }
        
        public static void Header(Rect rect, ref string header, DataViewHeader[] headers, out bool modified) {
            Header(rect, ref header, headers, out _, out modified);
        }
        
        public static void Header(Rect rect, ref string header, DataViewHeader[] headers, out int index, out bool modified) {
            var localHeader = header;
            index = headers.IndexOf(h => h.Name == localHeader);
            var newIndex = AREditorPopup.Draw(rect, index, headers, static h => h.NicePath, static h => h.NiceName);
            modified = index != newIndex;
            index = newIndex;
            header = index == -1 ? null : headers[index].Name;
        }
    }
}