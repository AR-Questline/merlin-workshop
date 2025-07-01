using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Utility.Localization;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews {
    public class DataViewSorterPopup {
        static readonly GUIContent[] DirectionLabels = { new("A-Z"), new("Z-A") };
        
        Comparer _comparer = new();
        GUIContent[] _headerNames;
        
        List<Entry> _entries = new();
        List<Entry> _newEntries = new();

        public void Clear() {
            _entries.Clear();
            _newEntries.Clear();
        }
        
        public void Refresh(DataViewHeader[] headers) {
            _headerNames = headers?.Select(h => h.NiceName).ToArray();
        }
        
        public void Draw(Rect rect, out bool changed) {
            var rects = new PropertyDrawerRects(rect);
            changed = false;

            if (_newEntries.Count == 0) {
                EditorGUI.LabelField(rects.AllocateLine(), "No Entries");
            } else {
                for (int i = 0; i < _newEntries.Count; i++) {
                    var entry = _newEntries[i];
                    var entryRects = new PropertyDrawerRects(rects.AllocateLine());
                    EditorGUI.BeginChangeCheck();
                    entry.header = EditorGUI.Popup(entryRects.AllocateWithRest(80), GUIContent.none, entry.header, _headerNames);
                    entry.direction = (Direction) EditorGUI.Popup(entryRects.AllocateLeft(50), GUIContent.none, (int) entry.direction, DirectionLabels);
                    _newEntries[i] = entry;
                    if (EditorGUI.EndChangeCheck()) {
                        changed = true;
                    }
                    
                    using (new ColorGUIScope(Color.red)) {
                        if (GUI.Button((Rect)entryRects, "X")) {
                            _newEntries.RemoveAt(i);
                            i--;
                            changed = true;
                        }
                    }
                }
            }

            rects.AllocateTop(5);

            if (GUI.Button(rects.AllocateLine(), "Add Entry")) {
                _newEntries.Add(default);
                changed = true;
            }
            if (GUI.Button(rects.AllocateLine(), "Clear")) {
                _newEntries.Clear();
                changed = true;
            }
        }
        
        public void ToggleSort(int header) {
            if (_newEntries.Count == 0) {
                _newEntries.Add(new Entry { header = header });
                return;
            }

            if (_newEntries[0].header != header) {
                _newEntries.RemoveAll(entry => entry.header == header);
                _newEntries.Insert(0, new Entry { header = header });
                return;
            }

            if (_newEntries[0].direction == Direction.Ascending) {
                _newEntries[0] = new Entry { header = header, direction = Direction.Descending };
            } else {
                _newEntries.RemoveAt(0);
            }
        }

        public void Sort(DataViewRow[] rows, DataViewHeader[] headers) {
            _entries.Clear();
            _entries.AddRange(_newEntries);
            _comparer.headers = headers;
            _comparer.entries = _entries;
            Array.Sort(rows, _comparer);
        }

        public static int Compare<T>(Direction direction, T lhs, T rhs) where T : struct, IComparable<T> {
            return direction switch {
                Direction.Ascending => lhs.CompareTo(rhs),
                Direction.Descending => rhs.CompareTo(lhs),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
        
        public static int Compare(Direction direction, string lhs, string rhs) {
            return direction switch {
                Direction.Ascending => string.Compare(lhs, rhs, StringComparison.InvariantCulture),
                Direction.Descending => string.Compare(rhs, lhs, StringComparison.InvariantCulture),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
        
        public static int Compare(Direction direction, Object lhs, Object rhs) {
            int lhsHash = lhs?.GetHashCode() ?? 0;
            int rhsHash = rhs?.GetHashCode() ?? 0;
            
            return direction switch {
                Direction.Ascending => lhsHash.CompareTo(rhsHash),
                Direction.Descending => rhsHash.CompareTo(lhsHash),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
        
        class Comparer : IComparer<DataViewRow> {
            public DataViewHeader[] headers;
            public List<Entry> entries;

            public int Compare(DataViewRow lhs, DataViewRow rhs) {
                if (lhs.source == rhs.source) return 0;
                if (lhs.source == null) return -1;
                if (rhs.source == null) return 1;
            
                foreach (var pair in entries) {
                    var result = headers[pair.header].Compare(lhs[pair.header], rhs[pair.header], pair.direction);
                    if (result != 0) {
                        return result;
                    }
                }
            
                return 0;
            }
        }

        struct Entry {
            public int header;
            public Direction direction;
        }
        
        public enum Direction : byte {
            Ascending,
            Descending,
        }
    }
}