using System;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Types;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Headers {
    public abstract class DataViewHeader {
        public string Name { get; init; }
        public GUIContent NiceName { get; init; }
        public GUIContent NicePath { get; init; }
        public float Width { get; init; }
        public bool SupportEditing { get; init; } = true;
        public DataViewType Type { get; init; }
        
        protected DataViewHeader() { }

        protected DataViewHeader(string name, string niceName, float width, DataViewType type) {
            Name = name;
            (NiceName, NicePath) = GetNiceNameAndPath(niceName);
            Width = width;
            Type = type;
        }

        public virtual UniversalPtr CreateMetadata(IDataViewSource o) => default;
        public virtual void FreeMetadata(ref UniversalPtr ptr) { }
        public abstract DataViewValue GetValue(in DataViewCell cell);
        public abstract void SetValue(in DataViewCell cell, in DataViewValue value);
        public abstract Object GetModifiedObject(in DataViewCell cell);

        public void Draw(in DataViewCell cell, in Rect rect) {
            var value = GetValue(cell);
            Type.Draw(rect, value, out var result, cell.typeMetadata, out var modified);
            if (modified && SupportEditing) {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName($"Edit {NiceName.text}");
                var modifiedObject = GetModifiedObject(cell);
                Undo.RecordObject(modifiedObject, "");
                SetValue(cell, result);
                EditorUtility.SetDirty(modifiedObject);
                Undo.IncrementCurrentGroup();
            }
        }

        public bool TryDrawBulk(in Rect rect, ref DataViewValue value, in UniversalPtr metadata) {
            if (SupportEditing) {
                Type.Draw(rect, value, out value, metadata, out _);
                return true;
            } else {
                EditorGUI.LabelField(rect, "N/A");
                return false;
            }
        }
        public void ApplyBulk(in DataViewCell cell, in DataViewValue value) {
            SetValue(cell, value);
        }

        public int Compare(in DataViewCell lhs, in DataViewCell rhs, DataViewSorterPopup.Direction direction) {
            return Type.Compare(direction, GetValue(lhs), GetValue(rhs));
        }

        public bool SupportsExporting() => Type.SupportExporting();
        public string Export(in DataViewCell cell) => Type.ToString(GetValue(cell));

        public void Import(in DataViewCell cell, ReadOnlySpan<char> value) {
            DataViewValue result = default;
            if (Type.TryParse(value, ref result)) {
                SetValue(cell, result);
                EditorUtility.SetDirty(cell.source.UnityObject);
            } else {
                Log.Important?.Error($"Cannot parse value {value.ToString()} as {Type}");
            }
        }
        
        public bool Equals(DataViewHeader other) {
            return Name == other.Name;
        }

        public static Func<Object, Object> ExtractComponent(Type type) => o => o switch {
            Component component => component.GetComponent(type),
            GameObject go => go.GetComponent(type),
            _ => null
        };

        public static T ExtractObject<T>(IDataViewSource source) => source.UnityObject switch {
            T t => t,
            Component component => component.GetComponent<T>(),
            GameObject go => go.GetComponent<T>(),
            _ => default
        };

        public static (GUIContent niceName, GUIContent nicePath) GetNiceNameAndPath(string nicePath) {
            int lastSlash = nicePath.LastIndexOf('/');
            return (new GUIContent(nicePath[(lastSlash + 1)..]), new GUIContent(nicePath));
        }
    }
}