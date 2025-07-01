using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Utils;
using Awaken.TG.Editor.Helpers;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Filters {
    [Serializable]
    public class DataViewFilterList {
        [SerializeField] List<SerializedEntry> entries = new();
        
        public List<Entry> GetEntries(DataViewHeader[] headers) {
            var results = new List<Entry>(entries.Count);
            foreach (var entry in entries) {
                var headerName = entry.name;
                var headerIndex = headers.IndexOf(h => h.Name == headerName);
                if (headerIndex != -1) {
                    results.Add(new Entry(headerIndex, headers[headerIndex], entry.filter));
                }
            }
            return results;
        }

        public IEnumerable<IDataViewSource> Filter(IEnumerable<IDataViewSource> objects, DataViewHeader[] possibleHeaders) {
            var filters = GetEntries(possibleHeaders);
            foreach (var o in objects) {
                if (filters.Count == 0 || filters.All(filter => filter.Match(o))) {
                    yield return o;
                }
            }
        }

        public void Clear() {
            entries.Clear();
        }

        public float DrawHeight() {
            float rows = 0;
            foreach (var entry in entries) {
                rows += EditorGUIUtility.singleLineHeight * 1.5f + (entry.filter?.DrawHeight() ?? 0f);
            }
            rows += EditorGUIUtility.singleLineHeight;
            return rows;
        }
        
        public void Draw(Rect rect, DataViewHeader[] possibleHeaders, ref bool changed) {
            RemoveInvalidEntries(possibleHeaders, ref changed);
            var rects = new PropertyDrawerRects(rect);
            for (int i = 0; i < entries.Count; i++) {
                var entry = entries[i];
                int header;
                { // draw header
                    var headerRects = new PropertyDrawerRects(rects.AllocateLine());
                    DataViewDrawing.Header(headerRects.AllocateWithRest(80), ref entry.name, possibleHeaders, out header, out var modified);
                    if (modified) {
                        entry.filter = header == -1 ? null : possibleHeaders[header].Type.GetFilter();
                        entries[i] = entry;
                        changed = true;
                    }
                    if (GUI.Button((Rect)headerRects, "X")) {
                        entries.RemoveAt(i);
                        i--;
                        changed = true;
                        continue;
                    }
                }
                if (header != -1) {
                    var filterRect = rects.AllocateTop(entry.filter!.DrawHeight());
                    entry.filter.Draw(filterRect, ref changed);
                }
                rects.AllocateTop(0.5f * EditorGUIUtility.singleLineHeight);
            }
            if (GUI.Button(rects.AllocateLine(), "Add Filter")) {
                entries.Add(new SerializedEntry());
                changed = true;
            }
        }

        void RemoveInvalidEntries(DataViewHeader[] possibleHeaders, ref bool changed) {
            for (int i = entries.Count - 1; i >= 0; i--) {
                var entry = entries[i];
                var headerName = entry.name;
                if (string.IsNullOrEmpty(headerName)) {
                    continue;
                }
                var header = possibleHeaders.FirstOrDefault(h => h.Name == headerName);
                if (header == null) {
                    entries.RemoveAt(i);
                    changed = true;
                }
            }
        }
        
        [Serializable]
        struct SerializedEntry {
            public string name;
            [SerializeReference] public DataViewFilter filter;
        }

        public readonly struct Entry {
            public readonly int headerIndex;
            public readonly DataViewHeader header;
            public readonly DataViewFilter filter;
            
            public Entry(int headerIndex, DataViewHeader header, DataViewFilter filter) {
                this.headerIndex = headerIndex;
                this.header = header;
                this.filter = filter;
            }
            
            public bool Match(IDataViewSource o) {
                var headerMetadata = header.CreateMetadata(o);
                var typeMetadata = header.Type.CreateMetadata();
                var match = filter.Match(header.GetValue(new DataViewCell(o, headerMetadata, typeMetadata)));
                header.Type.FreeMetadata(ref typeMetadata);
                header.FreeMetadata(ref headerMetadata);
                return match;
            }

            public bool Match(in DataViewRow row) {
                return filter.Match(header.GetValue(row[headerIndex]));
            }
        }
    }
}