using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.DataViews.Filters;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Utils;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Data {
    [Serializable]
    public class DataViewTab {
        public string name;
        public float rowHeight = 1;
        [RichEnumExtends(typeof(DataViewArchetype))] public RichEnumReference archetypeRef;
        public List<string> headers = new();
        public DataViewFilterList filterList = new();
        
        public DataViewArchetype Archetype => archetypeRef.EnumAs<DataViewArchetype>();
        public float RowHeight => rowHeight * EditorGUIUtility.singleLineHeight * DataViewPreferences.Instance.heightScale;

        public IEnumerable<DataViewHeader> GetHeaders() {
            var archetype = Archetype;
            var allHeaders = archetype.Headers;
            foreach (var header in this.headers) {
                var h = allHeaders.FirstOrDefault(h => h.Name == header);
                if (h != null) {
                    yield return h;
                }
            }
        }

        public IEnumerable<IDataViewSource> GetFilteredObjects() {
            var archetype = Archetype;
            return filterList.Filter(archetype.getCorrespondingObjects(), archetype.Headers);
        }
    }

    [CustomPropertyDrawer(typeof(DataViewTab))]
    public class DataViewTabEditor : PropertyDrawer {
        static readonly GUIContent RowHeightLabel = new("Row Height");
        static readonly GUIContent ArchetypeLabel = new("Archetype");
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (property.boxedValue is not DataViewTab tab) {
                return EditorGUIUtility.singleLineHeight;
            }
            float rows = 3 * EditorGUIUtility.singleLineHeight; // name + rowHeight + archetype;
            var archetype = tab.Archetype;
            if (archetype != null) {
                rows += EditorGUIUtility.singleLineHeight * 0.5f;               // space
                rows += EditorGUIUtility.singleLineHeight;                      // header(columns)
                rows += tab.headers.Count * EditorGUIUtility.singleLineHeight;  // headers
                rows += EditorGUIUtility.singleLineHeight;                      // button(add column)
                rows += EditorGUIUtility.singleLineHeight * 0.5f;               // space
                rows += EditorGUIUtility.singleLineHeight;                      // header(filters)
                rows += tab.filterList.DrawHeight();                            // filters
            }
            return rows;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.boxedValue is not DataViewTab tab) {
                return;
            }

            var propertyName = property.FindPropertyRelative("name");
            var propertyRowHeight = property.FindPropertyRelative("rowHeight");
            var propertyArchetype = property.FindPropertyRelative("archetypeRef");

            var rects = new PropertyDrawerRects(position);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rects.AllocateLine(), propertyName);
            EditorGUI.PropertyField(rects.AllocateLine(), propertyRowHeight, RowHeightLabel);
            EditorGUI.PropertyField(rects.AllocateLine(), propertyArchetype, ArchetypeLabel);
            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }

            bool modified = false;
            var archetype = tab.Archetype;
            if (archetype != null) {
                var allHeaders = archetype.Headers;
                rects.AllocateTop(EditorGUIUtility.singleLineHeight * 0.5f);
                EditorGUI.LabelField(rects.AllocateLine(), "Columns");
                for (int i = 0; i < tab.headers.Count; i++) {
                    var header = tab.headers[i];
                    if (!header.IsNullOrWhitespace() && allHeaders.All(h => h.Name != header)) {
                        tab.headers.RemoveAt(i);
                        i--;
                        modified = true;
                        continue;
                    }
                    var headerRects = new PropertyDrawerRects(rects.AllocateLine());
                    DataViewDrawing.Header(headerRects.AllocateLeftNormalized(0.5f), ref header, allHeaders, out var headerModified);
                    if (headerModified) {
                        tab.headers[i] = header;
                        modified = true;
                    }
                    if (GUI.Button(headerRects.AllocateLeftNormalized(0.2f), "<<")) {
                        if (i > 0) {
                            tab.headers.Insert(0, header);
                            tab.headers.RemoveAt(i + 1);
                            modified = true;
                        }
                    }
                    if (GUI.Button(headerRects.AllocateLeftNormalized(0.25f), "<")) {
                        if (i > 0) {
                            tab.headers.Insert(i - 1, header);
                            tab.headers.RemoveAt(i + 1);
                            modified = true;
                        }
                    }
                    if (GUI.Button(headerRects.AllocateLeftNormalized(0.33f), ">")) {
                        if (i < tab.headers.Count - 1) {
                            tab.headers.Insert(i + 2, header);
                            tab.headers.RemoveAt(i);
                            modified = true;
                        }
                    }
                    if (GUI.Button(headerRects.AllocateLeftNormalized(0.5f), ">>")) {
                        if (i < tab.headers.Count - 1) {
                            tab.headers.Insert(tab.headers.Count, header);
                            tab.headers.RemoveAt(i);
                            modified = true;
                        }
                    }
                    if (GUI.Button((Rect)headerRects, "X")) {
                        tab.headers.RemoveAt(i);
                        i--;
                        modified = true;
                    }
                }
                if (GUI.Button(rects.AllocateLine(), "Add Column")) {
                    tab.headers.Add(null);
                    modified = true;
                }
                rects.AllocateTop(EditorGUIUtility.singleLineHeight * 0.5f);
                EditorGUI.LabelField(rects.AllocateLine(), "Filters");
                tab.filterList.Draw((Rect)rects, allHeaders, ref modified);
            } else {
                if (tab.headers.Count > 0) {
                    tab.headers.Clear();
                    modified = true;
                }
            }

            if (modified) {
                // We have retrieved boxedValue of property so it was shallowly copied.
                // We need to Update serializeObject to apply changes made to indirectly referenced objects.
                // If we don't do this ApplyModifiedProperties would not see them as modified and erase them.
                property.serializedObject.Update();
                property.boxedValue = tab;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}