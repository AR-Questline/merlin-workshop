using Awaken.TG.Editor.DataViews.Data;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.Helpers;
using Awaken.Utility.LowLevel;
using Awaken.Utility.UI;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews {
    public struct DataViewBulkEditor {
        public static float Height => 2 * EditorGUIUtility.singleLineHeight;
        
        DataViewValue[] _dummyValues;
        UniversalPtr[] _metadata;
        
        public void Refresh(DataViewHeader[] headers) {
            _dummyValues = new DataViewValue[headers.Length];
            _metadata = new UniversalPtr[headers.Length];
            for (int i = 0; i < headers.Length; i++) {
                _metadata[i] = headers[i].Type.CreateMetadata();
            }
        }

        public void Clear(DataViewHeader[] headers) {
            if (headers == null) {
                return;
            }
            for (int i = 0; i < headers.Length; i++) {
                headers[i].Type.FreeMetadata(ref _metadata[i]);
            }
        }

        public void Draw(Rect namesRect, Rect contentRect, DataViewHeader[] headers, DataViewRow[] rows, int scrollXInt, ref string name, ref int selectedObjectCount) {
            var previousGUIBackground = GUI.backgroundColor;
            var mousePosition = Event.current.mousePosition;
            Color background;
            Color fieldColor;
            if (namesRect.Contains(mousePosition) || contentRect.Contains(mousePosition)) {
                background = DataViewPreferences.Instance.bgHover.background;
                fieldColor = DataViewPreferences.Instance.bgHover.fieldColor;
            } else {
                background = DataViewPreferences.Instance.bgNormal.background[0];
                fieldColor = DataViewPreferences.Instance.bgNormal.fieldColor[0];
            }
            EditorGUI.DrawRect(namesRect.Expand(DataView.Padding), background);
            EditorGUI.DrawRect(contentRect.Expand(DataView.Padding), background);
            GUI.backgroundColor = fieldColor;
            
            var contentRects = new PropertyDrawerRects(contentRect);
            var namesRects = new PropertyDrawerRects(namesRect);
            
            var selectionRect = new PropertyDrawerRects(namesRects.AllocateLeft(DataView.BulkSelectionWidth));
            if (GUI.Button(selectionRect.AllocateTop(EditorGUIUtility.singleLineHeight), "All")) {
                for (int i = 0; i < rows.Length; i++) {
                    rows[i].selected = true;
                }
                selectedObjectCount = rows.Length;
                name = $"Bulk Edit ({selectedObjectCount})";
            }
            if (GUI.Button(selectionRect.AllocateTop(EditorGUIUtility.singleLineHeight), "None")) {
                for (int i = 0; i < rows.Length; i++) {
                    rows[i].selected = false;
                }
                selectedObjectCount = 0;
                name = "Bulk Edit (0)";
            }
            namesRects.AllocateLeft(30);
            EditorGUI.LabelField((Rect)namesRects, name);
            
            using var disableScope = new EditorGUI.DisabledScope(selectedObjectCount == 0);
            for (int i = scrollXInt; i < headers.Length; i++) {
                var header = headers[i];
                contentRects.AllocateLeft(DataView.Padding);
                if (contentRects.TryAllocateLeft(header.Width, out var rect)) {
                    var rects = new PropertyDrawerRects(rect);
                    var fieldRect = rects.AllocateTop(EditorGUIUtility.singleLineHeight);
                    var buttonRect = (Rect)rects;
                    if (header.TryDrawBulk(fieldRect, ref _dummyValues[i], _metadata[i])) {
                        if (GUI.Button(buttonRect, "Apply")) {
                            var modifiedObjects = new Object[selectedObjectCount];
                            var selectedCells = new DataViewCell[selectedObjectCount];
                            int selectedIndex = 0;
                            for (int j = 0; j < rows.Length; j++) {
                                if (rows[j].selected) {
                                    selectedCells[selectedIndex] = rows[j][i];
                                    modifiedObjects[selectedIndex] = header.GetModifiedObject(rows[j][i]);
                                    selectedIndex++;
                                }
                            }
                            Undo.IncrementCurrentGroup();
                            Undo.SetCurrentGroupName($"Bulk Edit {header.NiceName.text}");
                            Undo.RecordObjects(modifiedObjects, "");
                            for (int j = 0; j < selectedObjectCount; j++) {
                                header.ApplyBulk(selectedCells[j], _dummyValues[i]);
                                EditorUtility.SetDirty(modifiedObjects[j]);
                            }
                            Undo.IncrementCurrentGroup();
                        }
                    }
                    contentRects.AllocateLeft(DataView.Padding);
                } else {
                    break;
                }
            }

            GUI.backgroundColor = previousGUIBackground;
        }
    }
}