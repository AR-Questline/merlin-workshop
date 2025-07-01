using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.Helpers;
using Awaken.Utility.UI;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews {
    public class DataViewExporterPopup {
        static readonly GUIContent FolderLabel = new("Folder");
        static readonly GUIContent FileNameLabel = new("File Name");

        string _folder;
        string _fileName = "data";

        public bool Draw(Rect rect, DataViewRow[] rows, DataViewHeader[] headers) {
            var rects = new PropertyDrawerRects(rect);
            // _folder = SirenixEditorFields.FolderPathField(rects.AllocateLine(), FolderLabel, _folder, null, false, false);
            rects.AllocateTop(1);
            // _fileName = SirenixEditorFields.TextField(rects.AllocateLine(), FileNameLabel, _fileName);
            rects.AllocateTop(5);
            if (GUI.Button(rects.AllocateLine(), "Export")) {
                Export(rows, headers);
                return false;
            }
            if (GUI.Button(rects.AllocateLine(), "Cancel")) {
                return false;
            }
            return true;
        }

        void Export(DataViewRow[] rows, DataViewHeader[] allHeaders) {
            using var stream = File.OpenWrite(Path.Combine(_folder, _fileName + DataView.ExportDataExtension));
            using var writer = new StreamWriter(stream);

            var headers = allHeaders.Select((h, i) => (h, i)).Where(pair => pair.h.SupportsExporting()).ToArray();

            var headersNames = PrependedHeaders.Concat(headers.Select(pair => pair.h.Name));
            writer.WriteLine(string.Join(DataView.ExportDataSeparator, headersNames));
            
            foreach (var row in rows) {
                var entries = GetPrependedValues(row.source).Concat(headers.Select(pair => pair.h.Export(row[pair.i])));
                writer.WriteLine(string.Join(DataView.ExportDataSeparator, entries));
            }
        }

        static readonly string[] PrependedHeaders = {
            "ID",
            "Name",
        };

        static IEnumerable<string> GetPrependedValues(IDataViewSource source) {
            yield return source.Id;
            yield return source.Name;
        }
    }
}